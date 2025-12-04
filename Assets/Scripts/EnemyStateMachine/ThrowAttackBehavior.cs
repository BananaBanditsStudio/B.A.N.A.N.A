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
    private bool hasFiredThisThrow = false;

    public void OnEnter(EnemyWithSM enemy)
    {
        enemyDamage = enemy.GetComponent<EnemyDamage>();
    }

    public void OnExit(EnemyWithSM enemy)
    {
        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Throw");
            enemy.Animator.speed = 1f; // Reset speed
        }
        isThrowing = false;
        hasFiredThisThrow = false;
        shootTimer = 0f;

        if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
        {
            enemy.Agent.ResetPath();
            enemy.Agent.isStopped = false;
        }
    }

    // Max distance at which enemy will stop and throw (otherwise chase closer)
    private const float MAX_ATTACK_RANGE = 20f;
    
    public void OnPerform(EnemyWithSM enemy, float deltaTime)
    {
        if (!isThrowing)
        {
            shootTimer += deltaTime;
        }

        float distanceToPlayer = enemy.Player != null 
            ? Vector3.Distance(enemy.transform.position, enemy.Player.transform.position) 
            : 0f;
        
        // If too far, chase closer before attacking
        if (distanceToPlayer > MAX_ATTACK_RANGE && !isThrowing)
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.isStopped = false;
                enemy.Agent.SetDestination(enemy.Player.transform.position);
            }
            return; // Don't attack yet, keep chasing
        }

        // Within attack range - stop and attack
        if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
        {
            enemy.Agent.isStopped = true;
            enemy.Agent.ResetPath();
        }

        if (enemy.Player != null)
        {
            enemy.transform.LookAt(enemy.Player.transform);
        }

        if (CanAttack(enemy) && shootTimer >= enemy.rangedFireRate)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.speed = enemy.throwAnimationSpeed;
                enemy.Animator.ResetTrigger("Throw");
                enemy.Animator.SetTrigger("Throw");
            }
            isThrowing = true;
            hasFiredThisThrow = false;
            throwStartTime = Time.time;
            shootTimer = 0f;

            EnemyAttackEvents attackEvents = enemy.Animator.GetComponent<EnemyAttackEvents>();
            if (attackEvents == null)
            {
                attackEvents = enemy.GetComponentInChildren<EnemyAttackEvents>();
            }
            if (attackEvents != null)
            {
                attackEvents.StartNewThrowCycle();
            }
        }

        CheckThrowAnimationComplete(enemy);

        if (isThrowing && enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            bool isInThrowAnimation = stateInfo.IsName("Throw") || stateInfo.IsName("Throwing");

            if (isInThrowAnimation && stateInfo.normalizedTime >= 0.5f && stateInfo.normalizedTime < 0.7f)
            {
                EnemyAttackEvents attackEvents = enemy.Animator.GetComponent<EnemyAttackEvents>();
                if (attackEvents == null)
                {
                    attackEvents = enemy.GetComponentInChildren<EnemyAttackEvents>();
                }

                if (attackEvents == null && !hasFiredThisThrow)
                {
                    Attack(enemy);
                    hasFiredThisThrow = true;
                }
            }
        }
    }

    public bool CanAttack(EnemyWithSM enemy)
    {
        if (enemyDamage != null && enemyDamage.IsSlipping())
        {
            return false;
        }

        if (isThrowing)
        {
            return false;
        }

        if (enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Throw") || stateInfo.IsName("Throwing"))
            {
                return false;
            }

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
        enemy.PlayThrowAttackSound();

        Transform gunBarrel = enemy.gunBarrel;

        if (gunBarrel == null || enemy.bulletPrefab == null)
        {
            return;
        }

        if (enemy.Player == null)
        {
            return;
        }

        // Get target position (head height)
        Vector3 playerPosition = enemy.Player.transform.position;
        playerPosition.y += 1.6f;
        
        // Add some random error for gameplay
        playerPosition += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), Random.Range(-0.5f, 0.5f));

        // Spawn at gunBarrel (no offset needed now)
        GameObject bullet = Object.Instantiate(enemy.bulletPrefab, gunBarrel.position, enemy.transform.rotation);

        if (bullet == null)
        {
            return;
        }

        // Ignore collisions between the wrench and the enemy
        Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
        Collider[] bulletColliders = bullet.GetComponentsInChildren<Collider>();
        foreach (Collider enemyCol in enemyColliders)
        {
            foreach (Collider bulletCol in bulletColliders)
            {
                Physics.IgnoreCollision(bulletCol, enemyCol);
            }
        }

        // Tell bullet where to go - it handles velocity itself
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.targetPosition = playerPosition;
            bulletScript.hasTarget = true;
        }
        
        hasFiredThisThrow = true;
    }

    public void UpdateMovement(EnemyWithSM enemy, float deltaTime)
    {
    }

    private void CheckThrowAnimationComplete(EnemyWithSM enemy)
    {
        if (isThrowing && enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            bool isInThrowAnimation = stateInfo.IsName("Throw") || stateInfo.IsName("Throwing");

            if (Time.time - throwStartTime > THROW_TIMEOUT)
            {
                isThrowing = false;
                shootTimer = 0f;
                enemy.Animator.speed = 1f; // Reset speed
            }
            else if (isInThrowAnimation && stateInfo.normalizedTime >= 0.95f)
            {
                // Only reset when throw animation is actually finishing (95%+)
                isThrowing = false;
                hasFiredThisThrow = false;
                enemy.Animator.speed = 1f; // Reset speed
            }
        }
    }

}
