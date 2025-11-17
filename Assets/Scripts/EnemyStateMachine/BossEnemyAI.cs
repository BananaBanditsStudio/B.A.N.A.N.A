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
    private bool wasInAttackAnimation = false; // Track previous frame's attack animation state
    private int[] attackAnimationHashes; // Cache animation hashes for performance
    
    private void Start()
    {
        enemyWithSM = GetComponent<EnemyWithSM>();
        enemyDamage = GetComponent<EnemyDamage>();
        
        // Cache animation hashes for better performance (avoids string comparisons)
        attackAnimationHashes = new int[]
        {
            Animator.StringToHash("BigJump"),
            Animator.StringToHash("BigMelee"),
            Animator.StringToHash("Melee")
        };
        
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
        else
        {
            // While enraged, periodically check if we should swap to the next attack
            // This allows the boss to cycle through different attack types
            CheckAndSwapEnragedAttack();
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
    
    /// <summary>
    /// Checks if the boss should swap to the next enraged attack.
    /// Swaps after each attack completes (detects transition from attack animation to idle/movement).
    /// </summary>
    private void CheckAndSwapEnragedAttack()
    {
        if (enemyWithSM == null || enragedPhaseAttacks.Length <= 1) return;
        
        // Check if enemy is in AttackState
        if (enemyWithSM.StateMachine != null && 
            enemyWithSM.StateMachine.activeState is AttackState)
        {
            if (enemyWithSM.Animator != null)
            {
                AnimatorStateInfo stateInfo = enemyWithSM.Animator.GetCurrentAnimatorStateInfo(0);
                int currentStateHash = stateInfo.shortNameHash;
                // Use cached hashes instead of string comparisons (much faster)
                bool isInAttackAnimation = currentStateHash == attackAnimationHashes[0] || 
                                          currentStateHash == attackAnimationHashes[1] || 
                                          currentStateHash == attackAnimationHashes[2];
                
                // Detect transition from attack animation to non-attack (attack just completed)
                if (wasInAttackAnimation && !isInAttackAnimation)
                {
                    // Attack just completed, swap to next attack type
                    AttackBehaviorType currentType = enemyWithSM.attackBehaviorType;
                    // Use Array.Exists instead of IndexOf for better performance
                    bool isCurrentEnragedAttack = System.Array.Exists(enragedPhaseAttacks, x => x == currentType);
                    
                    if (isCurrentEnragedAttack)
                    {
                        AttackBehaviorType nextAttack = GetNextEnragedAttack();
                        if (nextAttack != currentType)
                        {
                            enemyWithSM.attackBehaviorType = nextAttack;
                            Debug.Log($"BossEnemyAI: Attack completed, swapped to next enraged attack: {nextAttack}");
                        }
                    }
                }
                
                // Update tracking for next frame
                wasInAttackAnimation = isInAttackAnimation;
            }
        }
        else
        {
            // Not in attack state, reset tracking
            wasInAttackAnimation = false;
        }
    }
}

