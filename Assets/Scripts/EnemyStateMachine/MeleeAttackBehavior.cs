using UnityEngine;
using System.Collections;

public class MeleeAttackBehavior : IAttackBehavior
{
    private float lastAttackTime = 0f;
    private bool isAttackInProgress = false;
    private EnemyDamage enemyDamage;
    private Coroutine damageCoroutine;
    private MonoBehaviour coroutineRunner;
    private float attackRange;
    private float attackCooldown;
    private float damage;
    private float damageDelay;

    public void OnEnter(EnemyWithSM enemy)
    {
        enemyDamage = enemy.GetComponent<EnemyDamage>();
        coroutineRunner = enemy;

        attackRange = enemy.meleeAttackRange;
        attackCooldown = enemy.meleeAttackCooldown;
        damage = enemy.meleeDamage;
        damageDelay = enemy.meleeDamageDelay;

        if (enemy.Agent != null)
        {
            enemy.Agent.stoppingDistance = attackRange * 0.9f;
            enemy.Agent.autoBraking = true;
        }
    }

    public void OnExit(EnemyWithSM enemy)
    {
        if (isAttackInProgress)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.ResetTrigger("Melee");
            }

            if (damageCoroutine != null && coroutineRunner != null)
            {
                coroutineRunner.StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            isAttackInProgress = false;
        }

        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
            enemy.Agent.stoppingDistance = 0.1f;
        }
    }

    public void OnPerform(EnemyWithSM enemy, float deltaTime)
    {
        if (enemy.Player == null) return;

        Vector3 toPlayer = enemy.Player.transform.position - enemy.transform.position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, targetRotation, 720f * deltaTime);
        }

        bool isMeleeAnimPlaying = false;
        if (enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            isMeleeAnimPlaying = stateInfo.IsName("Melee");

            if (isMeleeAnimPlaying)
            {
                isAttackInProgress = true;
                if (enemy.Agent != null)
                {
                    enemy.Agent.isStopped = true;
                    enemy.Agent.ResetPath();
                }
            }
            else if (isAttackInProgress && !isMeleeAnimPlaying)
            {
                FinishAttack(enemy);
            }
        }

        if (!isMeleeAnimPlaying)
        {
            if (distanceToPlayer <= attackRange &&
                Time.time - lastAttackTime >= attackCooldown &&
                CanAttack(enemy))
            {
                StartAttack(enemy);
            }
            else
            {
                UpdateMovement(enemy, deltaTime);
            }
        }
    }

    public bool CanAttack(EnemyWithSM enemy)
    {
        if (enemyDamage != null && enemyDamage.IsSlipping())
        {
            return false;
        }

        if (enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Melee"))
            {
                return false;
            }

            if (stateInfo.IsName("Slip") ||
                stateInfo.IsName("Slipping") ||
                stateInfo.IsName("Stun") ||
                stateInfo.IsName("Death") ||
                stateInfo.IsName("Throw"))
            {
                return false;
            }
        }

        return true;
    }

    public void Attack(EnemyWithSM enemy)
    {
        // Left empty intentionally
    }

    private void StartAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = true;
        lastAttackTime = Time.time;

        // 🔥 PLAY MELEE ATTACK SOUND HERE
        enemy.PlayMeleeAttackSound();

        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
            enemy.Agent.isStopped = true;
        }

        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Melee");
            enemy.Animator.SetTrigger("Melee");
        }

        if (coroutineRunner != null)
        {
            damageCoroutine = coroutineRunner.StartCoroutine(DealDamageAfterDelay(enemy, damageDelay));
        }
    }

    private void CancelAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = false;

        if (damageCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        if (enemy.Agent != null)
        {
            enemy.Agent.isStopped = false;
        }
    }

    private void FinishAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = false;

        if (enemy.Agent != null)
        {
            enemy.Agent.isStopped = false;
            if (enemy.Player != null)
            {
                enemy.Agent.SetDestination(enemy.Player.transform.position);
            }
        }
    }

    private IEnumerator DealDamageAfterDelay(EnemyWithSM enemy, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isAttackInProgress && enemy.Player != null)
        {
            Vector3 toPlayer = enemy.Player.transform.position - enemy.transform.position;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;

            if (distanceToPlayer <= attackRange)
            {
                PlayerHealth playerHealth = enemy.Player.GetComponent<PlayerHealth>();
                if (playerHealth == null && enemy.Player.transform.parent != null)
                {
                    playerHealth = enemy.Player.transform.parent.GetComponent<PlayerHealth>();
                }
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
        }

        damageCoroutine = null;
    }

    public void UpdateMovement(EnemyWithSM enemy, float deltaTime)
    {
        if (enemy.Agent != null && enemy.Player != null && !isAttackInProgress)
        {
            Vector3 playerPosition = enemy.Player.transform.position;
            Vector3 toPlayer = playerPosition - enemy.transform.position;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;

            if (distanceToPlayer > enemy.Agent.stoppingDistance)
            {
                if (enemy.Agent.isStopped)
                {
                    enemy.Agent.isStopped = false;
                }

                float destinationDistance = Vector3.Distance(enemy.Agent.destination, playerPosition);

                bool shouldUpdate = destinationDistance > 0.2f ||
                                    !enemy.Agent.hasPath ||
                                    (enemy.Agent.isStopped && distanceToPlayer > enemy.Agent.stoppingDistance + 0.1f);

                if (shouldUpdate)
                {
                    enemy.Agent.SetDestination(playerPosition);
                }
            }
            else if (distanceToPlayer <= enemy.Agent.stoppingDistance &&
                     !enemy.Agent.isStopped &&
                     enemy.Agent.velocity.magnitude < 0.1f)
            {
                enemy.Agent.isStopped = true;
                enemy.Agent.ResetPath();
            }
        }
    }
}
