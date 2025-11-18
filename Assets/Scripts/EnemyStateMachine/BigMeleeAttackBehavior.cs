using UnityEngine;
using System.Collections;

public class BigMeleeAttackBehavior : IAttackBehavior
{
    private float lastAttackTime = 0f;
    private bool isAttackInProgress = false;
    private bool damageDealt = false; // Track if damage has been dealt for this attack
    private EnemyDamage enemyDamage;
    private Coroutine damageCoroutine;
    private MonoBehaviour coroutineRunner;
    private float attackRange;
    private float attackCooldown;
    private float damage;
    private float damageDelay;
    private float knockbackForce;
    
    public void OnEnter(EnemyWithSM enemy)
    {
        enemyDamage = enemy.GetComponent<EnemyDamage>();
        coroutineRunner = enemy;
        
        attackRange = enemy.bigMeleeAttackRange;
        attackCooldown = enemy.bigMeleeAttackCooldown;
        damage = enemy.bigMeleeDamage;
        damageDelay = enemy.bigMeleeDamageDelay;
        knockbackForce = enemy.bigMeleeKnockback;
        
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
                enemy.Animator.ResetTrigger("BigMelee");
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
            isMeleeAnimPlaying = stateInfo.IsName("BigMelee") || stateInfo.IsName("Melee");
            
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
            if (stateInfo.IsName("BigMelee") || stateInfo.IsName("Melee") || stateInfo.IsName("BigJump"))
            {
                return false;
            }
            
            if (stateInfo.IsName("Slip") || stateInfo.IsName("Slipping") || 
                stateInfo.IsName("Stun") || stateInfo.IsName("Death") ||
                stateInfo.IsName("Throw"))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void Attack(EnemyWithSM enemy)
    {
    }
    
    private void StartAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = true;
        damageDealt = false; // Reset damage flag for new attack
        lastAttackTime = Time.time;
        
        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
            enemy.Agent.isStopped = true;
        }
        
        if (enemy.Animator != null)
        {
            bool useBigMelee = Random.value > 0.5f;
            
            if (useBigMelee)
            {
                enemy.Animator.ResetTrigger("BigMelee");
                enemy.Animator.SetTrigger("BigMelee");
            }
            else
            {
                enemy.Animator.ResetTrigger("Melee");
                enemy.Animator.SetTrigger("Melee");
            }
        }
        
        if (coroutineRunner != null)
        {
            damageCoroutine = coroutineRunner.StartCoroutine(DealDamageAfterDelay(enemy, damageDelay));
        }
    }
    
    private void FinishAttack(EnemyWithSM enemy)
    {
        // Don't reset isAttackInProgress if damage hasn't been dealt yet
        // This allows the damage coroutine to complete even after animation finishes
        if (damageDealt)
        {
            isAttackInProgress = false;
        }
        
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
        
        // Check if attack is still valid (either in progress OR animation just finished but damage not dealt yet)
        if ((isAttackInProgress || !damageDealt) && enemy.Player != null)
        {
            float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Player.transform.position);
            
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
                    damageDealt = true; // Mark damage as dealt
                    
                    if (knockbackForce > 0f)
                    {
                        Rigidbody playerRb = enemy.Player.GetComponent<Rigidbody>();
                        if (playerRb != null)
                        {
                            Vector3 knockbackDirection = (enemy.Player.transform.position - enemy.transform.position).normalized;
                            playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                        }
                    }
                }
            }
        }
        
        // Now safe to reset attack state
        isAttackInProgress = false;
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


