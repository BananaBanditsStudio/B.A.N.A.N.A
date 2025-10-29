using UnityEngine;

public class AttackState : BaseState
{
    private float moveTimer = 0f;
    private float losePlayerTimer = 0f;
    private float shootTimer = 0f;
    public override void Enter()
    {
    }

    public override void Exit()
    {
        enemy.Animator.ResetTrigger("Throw");
    }

    public override void Perform()
    {
        if (enemy.CanSeePlayer()) {
            losePlayerTimer = 0f;
            moveTimer += Time.deltaTime;
            shootTimer += Time.deltaTime;
            enemy.transform.LookAt(enemy.Player.transform);
            
            if (shootTimer > enemy.fireRate) {
                enemy.Animator.ResetTrigger("Throw");
                enemy.Animator.SetTrigger("Throw");
                Shoot();
            }
            
            
            
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
