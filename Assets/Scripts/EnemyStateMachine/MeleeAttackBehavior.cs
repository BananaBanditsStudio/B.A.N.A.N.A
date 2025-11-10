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
        
        // Get melee attack settings from enemy
        attackRange = enemy.meleeAttackRange;
        attackCooldown = enemy.meleeAttackCooldown;
        damage = enemy.meleeDamage;
        damageDelay = enemy.meleeDamageDelay;
        
        // Configure agent for melee combat
        // Speed is already set to chaseSpeed in AttackState.Enter()
        if (enemy.Agent != null)
        {
            // Stop slightly before attack range to ensure we're in range for attacks
            enemy.Agent.stoppingDistance = attackRange * 0.9f;
            enemy.Agent.autoBraking = true;
        }
    }
    
    public void OnExit(EnemyWithSM enemy)
    {
        // Stop any ongoing attack
        if (isAttackInProgress)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.ResetTrigger("Melee");
            }
            
            // Stop damage coroutine if running
            if (damageCoroutine != null && coroutineRunner != null)
            {
                coroutineRunner.StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
            
            isAttackInProgress = false;
        }
        
        // Reset agent
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
        
        // Face the player
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, targetRotation, 720f * deltaTime);
        }
        
        // Check if Melee animation is currently playing
        bool isMeleeAnimPlaying = false;
        if (enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            isMeleeAnimPlaying = stateInfo.IsName("Melee");
            
            // If we're in Melee animation, we're attacking
            if (isMeleeAnimPlaying)
            {
                isAttackInProgress = true;
                // Keep agent stopped during animation
                if (enemy.Agent != null)
                {
                    enemy.Agent.isStopped = true;
                    enemy.Agent.ResetPath();
                }
            }
            // If we were attacking but not in Melee state anymore, attack finished
            else if (isAttackInProgress && !isMeleeAnimPlaying)
            {
                FinishAttack(enemy);
            }
        }
        
        // Only check for new attacks and movement if not currently in attack animation
        if (!isMeleeAnimPlaying)
        {
            // Check if we can attack (in range, cooldown ready, not already attacking, not slipping)
            if (distanceToPlayer <= attackRange && 
                Time.time - lastAttackTime >= attackCooldown && 
                CanAttack(enemy))
            {
                // Start attack
                StartAttack(enemy);
            }
            else
            {
                // Always update movement if not attacking
                // This ensures we track the player even when cooldown is active or slightly out of range
                UpdateMovement(enemy, deltaTime);
            }
        }
    }
    
    public bool CanAttack(EnemyWithSM enemy)
    {
        // Don't attack if slipping
        if (enemyDamage != null && enemyDamage.IsSlipping())
        {
            return false;
        }
        
        // Don't attack if already in Melee animation
        if (enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Melee"))
            {
                return false;
            }
            
            // Don't attack if in interrupting animations
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
        // This is called by animation events if needed, but we handle it in StartAttack
        // Keep this for compatibility
    }
    
    private void StartAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = true;
        lastAttackTime = Time.time;
        
        // Stop movement
        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
            enemy.Agent.isStopped = true;
        }
        
        // Trigger melee animation
        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Melee");
            enemy.Animator.SetTrigger("Melee");
        }
        
        // Start damage coroutine
        if (coroutineRunner != null)
        {
            damageCoroutine = coroutineRunner.StartCoroutine(DealDamageAfterDelay(enemy, damageDelay));
        }
    }
    
    private void CancelAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = false;
        
        // Stop damage coroutine
        if (damageCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
        
        // Resume movement
        if (enemy.Agent != null)
        {
            enemy.Agent.isStopped = false;
        }
    }
    
    private void FinishAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = false;
        
        // Resume movement and ensure destination is updated
        if (enemy.Agent != null)
        {
            enemy.Agent.isStopped = false;
            // Force update destination after attack to handle player movement
            if (enemy.Player != null)
            {
                enemy.Agent.SetDestination(enemy.Player.transform.position);
            }
        }
    }
    
    private IEnumerator DealDamageAfterDelay(EnemyWithSM enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only deal damage if attack is still in progress and player is still in range
        if (isAttackInProgress && enemy.Player != null)
        {
            Vector3 toPlayer = enemy.Player.transform.position - enemy.transform.position;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;
            
            // Only deal damage if player is still in attack range
            if (distanceToPlayer <= attackRange)
            {
                PlayerHealth playerHealth = enemy.Player.transform.parent.GetComponent<PlayerHealth>();
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
        // Move towards player using NavMesh
        if (enemy.Agent != null && enemy.Player != null && !isAttackInProgress)
        {
            Vector3 playerPosition = enemy.Player.transform.position;
            Vector3 toPlayer = playerPosition - enemy.transform.position;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;
            
            // If player is beyond attack range, we definitely need to move
            // If player is beyond stopping distance but within attack range, we should still move to get closer
            if (distanceToPlayer > enemy.Agent.stoppingDistance)
            {
                // Ensure agent is not stopped
                if (enemy.Agent.isStopped)
                {
                    enemy.Agent.isStopped = false;
                }
                
                // Calculate distance from current destination to new player position
                float destinationDistance = Vector3.Distance(enemy.Agent.destination, playerPosition);
                
                // Update destination if:
                // 1. Player has moved significantly from current destination (> 0.2f)
                // 2. We don't have a valid path
                // 3. We're stopped but player is beyond stopping distance (player moved away)
                bool shouldUpdate = destinationDistance > 0.2f || 
                                    !enemy.Agent.hasPath || 
                                    (enemy.Agent.isStopped && distanceToPlayer > enemy.Agent.stoppingDistance + 0.1f);
                
                if (shouldUpdate)
                {
                    enemy.Agent.SetDestination(playerPosition);
                }
            }
            // If player is within stopping distance but we're not attacking and cooldown isn't ready,
            // we might need to adjust position if player moved
            else if (distanceToPlayer <= enemy.Agent.stoppingDistance && 
                     !enemy.Agent.isStopped && 
                     enemy.Agent.velocity.magnitude < 0.1f)
            {
                // We've reached stopping distance, so stop the agent
                enemy.Agent.isStopped = true;
                enemy.Agent.ResetPath();
            }
        }
    }
}

