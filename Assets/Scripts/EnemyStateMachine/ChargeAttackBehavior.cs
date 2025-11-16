using UnityEngine;
using System.Collections;

/// <summary>
/// Attack behavior that charges at high speed towards the player and explodes on contact.
/// This is a kamikaze-style attack behavior.
/// </summary>
public class ChargeAttackBehavior : IAttackBehavior
{
    private bool hasExploded = false;
    private EnemyDamage enemyDamage;
    private float chargeSpeed;
    private float explosionRange;
    private float explosionDamage;

    public void OnEnter(EnemyWithSM enemy)
    {
        enemyDamage = enemy.GetComponent<EnemyDamage>();
        hasExploded = false;

        // Get charge attack settings from enemy
        chargeSpeed = enemy.chargeSpeed;
        explosionRange = enemy.explosionRange;
        explosionDamage = enemy.explosionDamage;

        // Configure agent for charging
        if (enemy.Agent != null)
        {
            enemy.Agent.speed = chargeSpeed;
            enemy.Agent.stoppingDistance = 0f;
            enemy.Agent.autoBraking = false;
            enemy.Agent.isStopped = false;
        }

        // Trigger run animation
        if (enemy.Animator != null)
        {
            enemy.Animator.SetFloat("speed", chargeSpeed);
        }
    }

    public void OnExit(EnemyWithSM enemy)
    {
        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
        }

        if (enemy.Animator != null)
        {
            enemy.Animator.SetFloat("speed", 0f);
        }
    }

    public void OnPerform(EnemyWithSM enemy, float deltaTime)
    {
        if (enemy.Player == null || hasExploded) return;

        Vector3 toPlayer = enemy.Player.transform.position - enemy.transform.position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;

        // Face the player
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, targetRotation, 1080f * deltaTime);
        }

        // Keep run animation active
        if (enemy.Animator != null)
        {
            enemy.Animator.SetFloat("speed", chargeSpeed);
        }

        float earlyExplosionRange = explosionRange * 2f;

        // Predict movement
        Vector3 velocity = enemy.Agent != null && enemy.Agent.enabled ? enemy.Agent.velocity : Vector3.zero;
        float speed = velocity.magnitude;
        float distanceThisFrame = speed * deltaTime;

        bool willBeInRange = distanceToPlayer <= (explosionRange + distanceThisFrame);

        if (distanceToPlayer <= earlyExplosionRange || willBeInRange)
        {
            Attack(enemy);
        }
        else
        {
            UpdateMovement(enemy, deltaTime);
        }
    }

    public bool CanAttack(EnemyWithSM enemy)
    {
        if (enemyDamage != null && enemyDamage.IsSlipping())
        {
            return false;
        }

        return !hasExploded;
    }

    public void Attack(EnemyWithSM enemy)
    {
        if (!hasExploded)
        {
            enemy.StartCoroutine(ExplosionSequence(enemy));
        }
    }

    /// ------------------------------------------------------
    /// NEW: Explosion sequence with dialogue before explosion
    /// ------------------------------------------------------
    private IEnumerator ExplosionSequence(EnemyWithSM enemy)
    {
        hasExploded = true;

        // Dialogue BEFORE explosion
        if (enemy.preExplosionDialogueClip != null && enemy.AudioSource != null)
        {
            enemy.AudioSource.PlayOneShot(enemy.preExplosionDialogueClip);
            yield return new WaitForSeconds(enemy.preExplosionDialogueClip.length);
        }

        // Now do the actual explosion
        ExplodeInstant(enemy);
    }

    /// ------------------------------------------------------
    /// Original explosion logic, unchanged but moved to a helper
    /// ------------------------------------------------------
    private void ExplodeInstant(EnemyWithSM enemy)
    {
        // Play explosion sound
        if (enemy != null)
        {
            enemy.PlayChargeAttackSound();
        }

        Vector3 explosionPosition = enemy.transform.position;

        // Spawn explosion FX
        if (enemy.explosionPrefab != null)
        {
            GameObject explosion = Object.Instantiate(enemy.explosionPrefab, explosionPosition, Quaternion.identity);
            Object.Destroy(explosion, 5f);
        }
        else
        {
            Debug.LogWarning("ChargeAttackBehavior: Explosion prefab is not set on EnemyWithSM!");
        }

        // Deal damage
        if (enemy.Player != null)
        {
            Vector3 toPlayer = enemy.Player.transform.position - explosionPosition;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;

            float damageRange = explosionRange * 2.5f;
            if (distanceToPlayer <= damageRange)
            {
                PlayerHealth playerHealth = enemy.Player.GetComponent<PlayerHealth>();
                if (playerHealth == null && enemy.Player.transform.parent != null)
                {
                    playerHealth = enemy.Player.transform.parent.GetComponent<PlayerHealth>();
                }
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(explosionDamage);
                }
            }
        }

        // Disable enemy after explosion
        enemy.enabled = false;

        StateMachine stateMachine = enemy.GetComponent<StateMachine>();
        if (stateMachine != null) stateMachine.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        if (enemyDamage != null)
        {
            enemyDamage.health = 0f;
            enemyDamage.enabled = false;
        }

        Object.Destroy(enemy.gameObject, 0.05f);
    }

    public void UpdateMovement(EnemyWithSM enemy, float deltaTime)
    {
        if (enemy.Agent != null && enemy.Player != null && !hasExploded)
        {
            if (enemy.Agent.isStopped)
            {
                enemy.Agent.isStopped = false;
            }

            Vector3 destination = enemy.Player.transform.position;
            enemy.Agent.SetDestination(destination);
        }
    }
}
