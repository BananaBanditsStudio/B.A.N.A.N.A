using UnityEngine;

public class AttackState : BaseState
{
    private float moveTimer = 0f;
    private float losePlayerTimer = 0f;
    private float shootTimer = 0f;
    private bool isThrowing = false;
    private float throwStartTime = 0f;
    private EnemyDamage enemyDamage;
    private const float MOVEMENT_THRESHOLD = 0.1f; // Minimum velocity to consider enemy as "moving"
    private const float THROW_TIMEOUT = 5f; // Maximum time to wait for throw animation event (safety fallback)
    
    public override void Enter()
    {
        enemyDamage = enemy.GetComponent<EnemyDamage>();
    }

    public override void Exit()
    {
        enemy.Animator.ResetTrigger("Throw");
        isThrowing = false;
    }

    public override void Perform()
    {
        if (enemy.CanSeePlayer()) {
            losePlayerTimer = 0f;
            moveTimer += Time.deltaTime;
            
            // Only increment shootTimer if we're NOT currently throwing
            // This prevents the timer from accumulating during the animation
            if (!isThrowing) {
                shootTimer += Time.deltaTime;
            }
            
            enemy.transform.LookAt(enemy.Player.transform);
            
            // Check if we can throw (not slipping, not already throwing, not moving)
            bool canThrow = CanThrow();
            
            if (canThrow && shootTimer > enemy.fireRate) {
                enemy.Animator.ResetTrigger("Throw");
                enemy.Animator.SetTrigger("Throw");
                isThrowing = true;
                throwStartTime = Time.time;
                // Reset timer immediately when we trigger the animation
                // The actual shooting will happen via animation event, but we want
                // the cooldown to start from when we trigger the animation, not when it fires
                shootTimer = 0f;
                
                // Start a new throw cycle to prevent double-firing from previous cycles
                // Use GetComponent (not GetComponentInChildren) to ensure we only get one component
                // The component should be on the same GameObject as the Animator
                EnemyAttackEvents attackEvents = enemy.Animator.GetComponent<EnemyAttackEvents>();
                if (attackEvents == null)
                {
                    // Fallback to GetComponentInChildren if not found on animator
                    attackEvents = enemy.GetComponentInChildren<EnemyAttackEvents>();
                }
                if (attackEvents != null)
                {
                    attackEvents.StartNewThrowCycle();
                }
                else
                {
                    Debug.LogError("AttackState: Could not find EnemyAttackEvents component!");
                }
            }
            
            // Check if throw animation has completed
            CheckThrowAnimationComplete();
            
            if (moveTimer > Random.Range(3f, 7f)) {
                enemy.Agent.SetDestination(enemy.transform.position + Random.insideUnitSphere * 5f);
                moveTimer = 0f;
            }
        } else {
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer >= 2f) {
                stateMachine.ChangeState(new PatrolState());
            }
        }
    }

    private bool CanThrow()
    {
        // Don't throw if enemy is slipping
        if (enemyDamage != null && enemyDamage.IsSlipping()) {
            return false;
        }
        
        // Don't throw if already throwing
        if (isThrowing) {
            return false;
        }
        
        // Don't throw if enemy is actively moving to a new position
        if (enemy.Agent.velocity.magnitude > MOVEMENT_THRESHOLD) {
            return false;
        }
        
        // Don't throw if currently in a throw animation
        AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Throw") || stateInfo.IsName("Throwing")) {
            return false;
        }
        
        // Don't throw if in other interrupting animations (slipping, stun, dead)
        if (stateInfo.IsName("Slip") || stateInfo.IsName("Slipping") || 
            stateInfo.IsName("Stun") || stateInfo.IsName("Death")) {
            return false;
        }
        
        return true;
    }

    private void CheckThrowAnimationComplete()
    {
        if (isThrowing) {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we're still in a throw animation by checking the state name
            // Also check if the animation has completed (normalizedTime >= 1)
            bool isInThrowAnimation = stateInfo.IsName("Throw") || stateInfo.IsName("Throwing");
            
            // Safety timeout: if animation event didn't fire after reasonable time, reset
            if (Time.time - throwStartTime > THROW_TIMEOUT)
            {
                Debug.LogWarning("AttackState: Throw animation timeout - resetting isThrowing flag");
                isThrowing = false;
                shootTimer = 0f; // Reset timer to prevent immediate retry
            }
            // If we're no longer in a throw animation, reset the throwing flag
            else if (!isInThrowAnimation || stateInfo.normalizedTime >= 1f) {
                isThrowing = false;
            }
        }
    }

    public void Shoot() {
        // OPTIMIZED: Removed Debug.Log for minimal delay
        Transform gunBarrel = enemy.gunBarrel;

        // Instantiate the bullet
        GameObject bullet = Object.Instantiate(enemy.bulletPrefab, gunBarrel.position, enemy.transform.rotation);
        
        // Calculate the direction to the player
        Vector3 playerPosition = enemy.Player.transform.position;
        playerPosition.y += 0.8f;
        Vector3 shootDirection = (playerPosition - gunBarrel.position).normalized;
        
        // Add force to the bullet
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = Quaternion.AngleAxis(Random.Range(-3f, 3f), Vector3.up) * shootDirection * 40;
        }
        // Note: shootTimer is already reset when we trigger the animation
        // This ensures the cooldown starts from animation trigger, not from projectile spawn
    }
}
