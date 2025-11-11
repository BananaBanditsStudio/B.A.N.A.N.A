using UnityEngine;
using System.Collections;

public class BigJumpAttackBehavior : IAttackBehavior
{
    private float lastAttackTime = 0f;
    private bool isAttackInProgress = false;
    private EnemyDamage enemyDamage;
    private Coroutine damageCoroutine;
    private MonoBehaviour coroutineRunner;
    private float attackRange;
    private float attackCooldown;
    private float damage;
    private float jumpDamageDelay;
    private float aoeRadius;
    
    public void OnEnter(EnemyWithSM enemy)
    {
        enemyDamage = enemy.GetComponent<EnemyDamage>();
        coroutineRunner = enemy;
        
        attackRange = enemy.bigJumpRange;
        attackCooldown = enemy.bigJumpCooldown;
        damage = enemy.bigJumpDamage;
        jumpDamageDelay = enemy.bigJumpDamageDelay;
        aoeRadius = enemy.bigJumpAOERadius;
        
        if (enemy.Agent != null)
        {
            enemy.Agent.stoppingDistance = attackRange * 0.8f;
            enemy.Agent.autoBraking = true;
        }
    }
    
    public void OnExit(EnemyWithSM enemy)
    {
        if (isAttackInProgress)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.ResetTrigger("BigJump");
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
        
        bool isJumpAnimPlaying = false;
        if (enemy.Animator != null)
        {
            AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
            isJumpAnimPlaying = stateInfo.IsName("BigJump");
            
            if (isJumpAnimPlaying)
            {
                isAttackInProgress = true;
                if (enemy.Agent != null)
                {
                    enemy.Agent.isStopped = true;
                    enemy.Agent.ResetPath();
                }
            }
            else if (isAttackInProgress && !isJumpAnimPlaying)
            {
                FinishAttack(enemy);
            }
        }
        
        if (!isJumpAnimPlaying)
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
            if (stateInfo.IsName("BigJump") || stateInfo.IsName("BigMelee"))
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
    }
    
    private void StartAttack(EnemyWithSM enemy)
    {
        isAttackInProgress = true;
        lastAttackTime = Time.time;
        
        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
            enemy.Agent.isStopped = true;
        }
        
        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("BigJump");
            enemy.Animator.SetTrigger("BigJump");
        }
        
        if (coroutineRunner != null)
        {
            damageCoroutine = coroutineRunner.StartCoroutine(DealDamageAfterDelay(enemy, jumpDamageDelay));
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
            ShakeCamera(enemy);
            
            float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Player.transform.position);
            
            if (distanceToPlayer <= aoeRadius)
            {
                PlayerHealth playerHealth = enemy.Player.transform.parent.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
            
            yield return new WaitForSeconds(0.15f);
            
            if (enemy.bigJumpEffectPrefab != null)
            {
                GameObject effect = Object.Instantiate(enemy.bigJumpEffectPrefab, enemy.transform.position, Quaternion.identity);
                Object.Destroy(effect, 5f);
            }
        }
        
        damageCoroutine = null;
    }
    
    private void ShakeCamera(EnemyWithSM enemy)
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraShaker shaker = mainCam.GetComponent<CameraShaker>();
            if (shaker == null)
            {
                shaker = mainCam.gameObject.AddComponent<CameraShaker>();
            }
            shaker.ShakeCamera(enemy.bigJumpShakeIntensity, enemy.bigJumpShakeDuration);
        }
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

