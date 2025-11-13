using UnityEngine;

/// <summary>
/// Stun state that stops the enemy and plays a stun animation.
/// After the stun duration, transitions back to appropriate state.
/// </summary>
public class StunState : BaseState
{
    private float stunDuration;
    private float stunTimer = 0f;
    private bool hasTriggeredAnimation = false;
    
    // Constructor to set the stun duration when creating the state
    public StunState(float duration)
    {
        stunDuration = duration;
    }
    
    public override void Enter()
    {
        stunTimer = 0f;
        hasTriggeredAnimation = false;
        
        // Stop the agent immediately
        if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
        {
            enemy.Agent.isStopped = true;
            enemy.Agent.ResetPath();
        }
        
        // Trigger stun animation
        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Stun");
            enemy.Animator.SetTrigger("Stun");
            hasTriggeredAnimation = true;
        }
    }
    
    public override void Exit()
    {
        // Reset stun trigger
        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Stun");
        }
        
        // Resume agent movement
        if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
        {
            enemy.Agent.isStopped = false;
        }
    }
    
    public override void Perform()
    {
        // Safety check: ensure agent is available
        if (enemy.Agent == null || !enemy.Agent.isActiveAndEnabled)
        {
            // If agent is invalid, fall back to patrol
            stateMachine.ChangeState(new PatrolState());
            return;
        }
        
        // Update stun timer
        stunTimer += Time.deltaTime;
        
        // Check if stun duration has elapsed
        if (stunTimer >= stunDuration)
        {
            // Stun complete, transition to appropriate state
            // Check if player is visible - if so, go to attack, otherwise patrol
            if (enemy.CanSeePlayer())
            {
                stateMachine.ChangeState(new AttackState());
            }
            else
            {
                stateMachine.ChangeState(new PatrolState());
            }
        }
    }
}

