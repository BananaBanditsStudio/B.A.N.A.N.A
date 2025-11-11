using UnityEngine;

public class BossEnemyAI : MonoBehaviour
{
    [Header("Phase 1 - Normal")]
    public AttackBehaviorType normalPhaseAttack = AttackBehaviorType.Melee;
    
    [Header("Phase 2 - Enraged")]
    public float enragedHealthThreshold = 0.5f;
    public AttackBehaviorType[] enragedPhaseAttacks = new AttackBehaviorType[] { 
        AttackBehaviorType.BigJump, 
        AttackBehaviorType.BigMelee 
    };
    public float enragedSpeedMultiplier = 1.5f;
    
    private EnemyWithSM enemyWithSM;
    private EnemyDamage enemyDamage;
    private bool isEnraged = false;
    private float originalChaseSpeed;
    private float originalPatrolSpeed;
    private int lastEnragedAttackIndex = -1;
    
    private void Start()
    {
        enemyWithSM = GetComponent<EnemyWithSM>();
        enemyDamage = GetComponent<EnemyDamage>();
        
        if (enemyWithSM != null)
        {
            originalChaseSpeed = enemyWithSM.chaseSpeed;
            originalPatrolSpeed = enemyWithSM.patrolSpeed;
            enemyWithSM.attackBehaviorType = normalPhaseAttack;
        }
    }
    
    private void Update()
    {
        if (!isEnraged)
        {
            CheckEnragedPhase();
        }
    }
    
    
    public AttackBehaviorType GetNextEnragedAttack()
    {
        if (enragedPhaseAttacks.Length == 0)
            return AttackBehaviorType.Melee;
        
        if (enragedPhaseAttacks.Length == 1)
            return enragedPhaseAttacks[0];
        
        int newIndex;
        do
        {
            newIndex = Random.Range(0, enragedPhaseAttacks.Length);
        } while (newIndex == lastEnragedAttackIndex && enragedPhaseAttacks.Length > 1);
        
        lastEnragedAttackIndex = newIndex;
        return enragedPhaseAttacks[newIndex];
    }
    
    private void CheckEnragedPhase()
    {
        if (enemyDamage == null) return;
        
        float healthPercent = enemyDamage.health / enemyDamage.maxHealth;
        
        if (healthPercent <= enragedHealthThreshold)
        {
            EnterEnragedPhase();
        }
    }
    
    private void EnterEnragedPhase()
    {
        isEnraged = true;
        lastEnragedAttackIndex = -1;
        
        if (enemyWithSM != null)
        {
            enemyWithSM.chaseSpeed = originalChaseSpeed * enragedSpeedMultiplier;
            enemyWithSM.patrolSpeed = originalPatrolSpeed * enragedSpeedMultiplier;
            
            if (enemyWithSM.Agent != null)
            {
                enemyWithSM.Agent.speed = enemyWithSM.chaseSpeed;
            }
            
            enemyWithSM.attackBehaviorType = GetNextEnragedAttack();
        }
    }
    
    public bool IsEnraged()
    {
        return isEnraged;
    }
}

