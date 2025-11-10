using UnityEngine;

/// <summary>
/// Attack behavior that throws projectiles at the player.
/// This is the current/default attack behavior.
/// </summary>
public class ThrowAttackBehavior : IAttackBehavior
{
    private float shootTimer = 0f;
    private bool isThrowing = false;
    private float throwStartTime = 0f;
    private EnemyDamage enemyDamage;
    private const float THROW_TIMEOUT = 5f;
    
    public void OnEnter(EnemyWithSM enemy)
    {
        enemyDamage = enemy.GetComponent<EnemyDamage>();
    }
    
    public void OnExit(EnemyWithSM enemy)
    {
        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Throw");
        }
        isThrowing = false;
        shootTimer = 0f;
        
        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
            enemy.Agent.isStopped = false;
        }
    }
    
    public void OnPerform(EnemyWithSM enemy, float deltaTime)
    {
        // Only increment shootTimer if we're NOT currently throwing
        if (!isThrowing)
        {
            shootTimer += deltaTime;
        }
        
        // Stop movement and face player
        if (enemy.Agent != null)
        {
            enemy.Agent.isStopped = true;
            enemy.Agent.ResetPath();
        }
        
        if (enemy.Player != null)
        {
            enemy.transform.LookAt(enemy.Player.transform);
        }
        
        // Check if we can throw and fire rate is ready
        if (CanAttack(enemy) && shootTimer >= enemy.rangedFireRate)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.ResetTrigger("Throw");
                enemy.Animator.SetTrigger("Throw");
            }
            isThrowing = true;
            throwStartTime = Time.time;
            shootTimer = 0f;
            
            // Start a new throw cycle
            EnemyAttackEvents attackEvents = enemy.Animator.GetComponent<EnemyAttackEvents>();
            if (attackEvents == null)
            {
                attackEvents = enemy.GetComponentInChildren<EnemyAttackEvents>();
            }
            if (attackEvents != null)
            {
                attackEvents.StartNewThrowCycle();
            }
            else
            {
                Debug.LogError("ThrowAttackBehavior: Could not find EnemyAttackEvents component!");
            }
        }
        
        // Check if throw animation has completed
        CheckThrowAnimationComplete(enemy);
    }
    
    public bool CanAttack(EnemyWithSM enemy)
    {
        // Don't throw if enemy is slipping
        if (enemyDamage != null && enemyDamage.IsSlipping())
        {
            return false;
        }
        
        // Don't throw if already throwing
        if (isThrowing)
        {
            return false;
        }
        
        // Don't throw if currently in a throw animation
        if (enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Throw") || stateInfo.IsName("Throwing"))
            {
                return false;
            }
            
            // Don't throw if in other interrupting animations
            if (stateInfo.IsName("Slip") || stateInfo.IsName("Slipping") || 
                stateInfo.IsName("Stun") || stateInfo.IsName("Death"))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void Attack(EnemyWithSM enemy)
    {
        // This is called by EnemyAttackEvents, same as the old Shoot() method
        Transform gunBarrel = enemy.gunBarrel;
        
        if (gunBarrel == null || enemy.bulletPrefab == null)
        {
            Debug.LogWarning("ThrowAttackBehavior: Missing gunBarrel or bulletPrefab!");
            return;
        }
        
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
    }
    
    public void UpdateMovement(EnemyWithSM enemy, float deltaTime)
    {
        // Ranged enemies stay stationary and just throw
        // Movement is handled in OnPerform by stopping the agent
    }
    
    private void CheckThrowAnimationComplete(EnemyWithSM enemy)
    {
        if (isThrowing && enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            bool isInThrowAnimation = stateInfo.IsName("Throw") || stateInfo.IsName("Throwing");
            
            // Safety timeout
            if (Time.time - throwStartTime > THROW_TIMEOUT)
            {
                Debug.LogWarning("ThrowAttackBehavior: Throw animation timeout - resetting isThrowing flag");
                isThrowing = false;
                shootTimer = 0f;
            }
            else if (!isInThrowAnimation || stateInfo.normalizedTime >= 1f)
            {
                isThrowing = false;
            }
        }
    }
}

