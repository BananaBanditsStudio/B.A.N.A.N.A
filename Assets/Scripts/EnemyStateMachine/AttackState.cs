using UnityEngine;

/// <summary>
/// Attack state that uses a configurable attack behavior.
/// The behavior is swappable via EnemyWithSM.attackBehavior field.
/// </summary>
public class AttackState : BaseState
{
    private float losePlayerTimer = 0f;
    private IAttackBehavior attackBehavior;
    private AttackBehaviorType currentBehaviorType; // Track current behavior type to detect changes
    
    public override void Enter()
    {
        // Set chase speed (run speed) when attacking/chasing
        if (enemy.Agent != null)
        {
            enemy.Agent.speed = enemy.chaseSpeed;
            enemy.Agent.isStopped = false;
        }
        
        // Create a new attack behavior instance based on the enemy's attackBehaviorType
        // This allows swapping behaviors at runtime and ensures fresh state
        currentBehaviorType = enemy.attackBehaviorType;
        attackBehavior = AttackBehaviorFactory.Create(enemy.attackBehaviorType);
        attackBehavior.OnEnter(enemy);
    }

    public override void Exit()
    {
        if (attackBehavior != null)
        {
            attackBehavior.OnExit(enemy);
        }
        
        // Clear any movement paths when leaving attack state
        if (enemy.Agent != null)
        {
            enemy.Agent.ResetPath();
        }
    }

    public override void Perform()
    {
        // Check if attack behavior type has changed (e.g., boss phase transition)
        // If so, swap to the new behavior seamlessly
        if (enemy.attackBehaviorType != currentBehaviorType)
        {
            SwapAttackBehavior(enemy.attackBehaviorType);
        }
        
        if (enemy.CanSeePlayer())
        {
            losePlayerTimer = 0f;
            
            // Delegate attack logic to the behavior
            if (attackBehavior != null)
            {
                attackBehavior.OnPerform(enemy, Time.deltaTime);
            }
        }
        else
        {
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer >= 2f)
            {
                stateMachine.ChangeState(new PatrolState());
            }
        }
    }
    
    /// <summary>
    /// Swaps the attack behavior to a new type while remaining in AttackState.
    /// Used for boss phase transitions and other runtime behavior changes.
    /// </summary>
    private void SwapAttackBehavior(AttackBehaviorType newType)
    {
        // Exit the old behavior
        if (attackBehavior != null)
        {
            attackBehavior.OnExit(enemy);
        }
        
        // Create and enter the new behavior
        currentBehaviorType = newType;
        attackBehavior = AttackBehaviorFactory.Create(newType);
        if (attackBehavior != null)
        {
            attackBehavior.OnEnter(enemy);
            Debug.Log($"AttackState: Swapped attack behavior to {newType}");
        }
    }
    
    /// <summary>
    /// Called by EnemyAttackEvents to execute the attack.
    /// This maintains compatibility with the animation event system.
    /// </summary>
    public void Shoot()
    {
        if (attackBehavior != null)
        {
            attackBehavior.Attack(enemy);
        }
    }
    
    public void SetAttackBehavior(IAttackBehavior newBehavior)
    {
        if (attackBehavior != null)
        {
            attackBehavior.OnExit(enemy);
        }
        attackBehavior = newBehavior;
        if (attackBehavior != null)
        {
            attackBehavior.OnEnter(enemy);
        }
    }
}
