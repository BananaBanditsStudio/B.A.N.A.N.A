using UnityEngine;

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
            // Set very high speed for charging
            enemy.Agent.speed = chargeSpeed;
            enemy.Agent.stoppingDistance = 0f; // Don't stop, we want to get as close as possible
            enemy.Agent.autoBraking = false; // Don't brake, maintain speed
            enemy.Agent.isStopped = false;
        }
        
        // Explicitly set animator speed parameter to trigger run animation in blend tree
        // Blend tree thresholds: 0=idle, 0.1=walk, 7=run, 12=fast run
        // Charge speed (15f) should trigger the run animation
        if (enemy.Animator != null)
        {
            enemy.Animator.SetFloat("speed", chargeSpeed);
        }
    }
    
    public void OnExit(EnemyWithSM enemy)
    {
        // Cleanup if needed (though enemy should be destroyed after explosion)
        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
        }
        
        // Reset animator speed (though enemy should be destroyed after explosion)
        // This is just for cleanup in case the state exits without exploding
        if (enemy.Animator != null)
        {
            // Reset to 0 or let EnemyWithSM.Update() handle it
            enemy.Animator.SetFloat("speed", 0f);
        }
    }
    
    public void OnPerform(EnemyWithSM enemy, float deltaTime)
    {
        if (enemy.Player == null || hasExploded) return;
        
        Vector3 toPlayer = enemy.Player.transform.position - enemy.transform.position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;
        
        // Face the player while charging
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, targetRotation, 1080f * deltaTime);
        }
        
        // Keep animator speed parameter set to charge speed for run animation
        // This ensures the blend tree stays in run animation during charge
        if (enemy.Animator != null)
        {
            enemy.Animator.SetFloat("speed", chargeSpeed);
        }
        
        // Calculate early explosion trigger range (explode before getting too close)
        // This prevents the enemy from overshooting at high speeds
        float earlyExplosionRange = explosionRange * 2f;
        
        // Predictive check: if moving fast towards player, explode early
        // Calculate how far we'll travel this frame based on current velocity
        Vector3 velocity = enemy.Agent != null && enemy.Agent.enabled ? enemy.Agent.velocity : Vector3.zero;
        float speed = velocity.magnitude;
        float distanceThisFrame = speed * deltaTime;
        
        // If we're moving fast and will be within explosion range next frame, explode now
        bool willBeInRange = distanceToPlayer <= (explosionRange + distanceThisFrame);
        
        // Check if we're close enough to explode (early trigger)
        if (distanceToPlayer <= earlyExplosionRange || willBeInRange)
        {
            Explode(enemy);
        }
        else
        {
            // Move towards player
            UpdateMovement(enemy, deltaTime);
        }
    }
    
    public bool CanAttack(EnemyWithSM enemy)
    {
        // Always ready to charge (no cooldown needed)
        if (enemyDamage != null && enemyDamage.IsSlipping())
        {
            return false;
        }
        
        return !hasExploded;
    }
    
    public void Attack(EnemyWithSM enemy)
    {
        // This is called if needed, but explosion is handled in OnPerform
        Explode(enemy);
    }
    
    private void Explode(EnemyWithSM enemy)
    {
        if (hasExploded) return;
        hasExploded = true;
        
        Vector3 explosionPosition = enemy.transform.position;
        
        // Spawn explosion effect
        if (enemy.explosionPrefab != null)
        {
            GameObject explosion = Object.Instantiate(enemy.explosionPrefab, explosionPosition, Quaternion.identity);
            // Auto-destroy explosion after 5 seconds if it doesn't destroy itself
            Object.Destroy(explosion, 5f);
        }
        else
        {
            Debug.LogWarning("ChargeAttackBehavior: Explosion prefab is not set on EnemyWithSM!");
        }
        
        // Deal damage to player if in range
        // Use a generous damage range to ensure player gets hit even if explosion triggers early
        if (enemy.Player != null)
        {
            Vector3 toPlayer = enemy.Player.transform.position - explosionPosition;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;
            
            // Use a larger range for damage to account for early explosion trigger
            float damageRange = explosionRange * 2.5f;
            if (distanceToPlayer <= damageRange)
            {
                PlayerHealth playerHealth = enemy.Player.transform.parent.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(explosionDamage);
                }
            }
        }
        
        // Immediately disable all enemy components to prevent any further behavior
        if (enemy != null)
        {
            enemy.enabled = false;
        }
        
        StateMachine stateMachine = enemy.GetComponent<StateMachine>();
        if (stateMachine != null)
        {
            stateMachine.enabled = false;
        }
        
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        // Mark enemy as dead and disable components to prevent further behavior
        if (enemyDamage != null)
        {
            // Set health to 0 to mark as dead
            enemyDamage.health = 0f;
            // Disable EnemyDamage to prevent any death animation coroutines
            enemyDamage.enabled = false;
        }
        
        // Destroy the enemy after a tiny delay to ensure explosion spawns
        // This allows the explosion to be instantiated before the enemy is destroyed
        Object.Destroy(enemy.gameObject, 0.05f);
    }
    
    public void UpdateMovement(EnemyWithSM enemy, float deltaTime)
    {
        // Charge directly towards player
        if (enemy.Agent != null && enemy.Player != null && !hasExploded)
        {
            // Ensure agent is not stopped
            if (enemy.Agent.isStopped)
            {
                enemy.Agent.isStopped = false;
            }
            
            // Always update destination to player's current position
            Vector3 destination = enemy.Player.transform.position;
            enemy.Agent.SetDestination(destination);
        }
    }
}

