using UnityEngine;

/// <summary>
/// Attack state that uses a configurable attack behavior.
/// The behavior is swappable via EnemyWithSM.attackBehavior field.
/// </summary>
public class AttackState : BaseState
{
    private float losePlayerTimer = 0f;
    private IAttackBehavior attackBehavior;
    
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
}
