using UnityEngine;

public class AttackState : BaseState
{
    private float moveTimer = 0f;
    private float losePlayerTimer = 0f;
    private float shootTimer = 0f;
    private bool isThrowing = false;
    private EnemyDamage enemyDamage;
    private const float MOVEMENT_THRESHOLD = 0.1f; // Minimum velocity to consider enemy as "moving"
    
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
            shootTimer += Time.deltaTime;
            enemy.transform.LookAt(enemy.Player.transform);
            
            // Check if we can throw (not slipping, not already throwing, not moving)
            bool canThrow = CanThrow();
            
            if (canThrow && shootTimer > enemy.fireRate) {
                enemy.Animator.ResetTrigger("Throw");
                enemy.Animator.SetTrigger("Throw");
                isThrowing = true;
                Shoot();
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
            
            // If we're no longer in a throw animation, reset the throwing flag
            if (!stateInfo.IsName("Throw") && !stateInfo.IsName("Throwing")) {
                isThrowing = false;
            }
        }
    }

    public void Shoot() {
        Debug.Log("Shooting!");
        Transform gunBarrel = enemy.gunBarrel;

        // Instantiate the bullet
        GameObject bullet = Object.Instantiate(enemy.bulletPrefab, gunBarrel.position, enemy.transform.rotation);
        // Calculate the direction to the player
        Vector3 playerPosition = enemy.Player.transform.position;
        playerPosition.y += 0.8f;
        Vector3 shootDirection = (playerPosition - gunBarrel.position).normalized;
        // Add force to the bullet
        bullet.GetComponent<Rigidbody>().linearVelocity = Quaternion.AngleAxis(Random.Range(-3f, 3f), Vector3.up) * shootDirection * 40;
        shootTimer = 0f;
    }
}
