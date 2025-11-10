using UnityEngine;

public class DistractionState : BaseState
{
    private Vector3 targetPosition;
    private bool hasReachedDestination = false;
    private bool hasPlayedAnimation = false;
    private float animationTimer = 0f;
    private const float DESTINATION_THRESHOLD = 0.5f; // Distance threshold to consider destination reached
    private const float ANIMATION_DURATION = 8f; // Duration of confused animation (8 seconds)
    
    // Constructor to set the target position when creating the state
    public DistractionState(Vector3 target)
    {
        targetPosition = target;
    }
    
    public override void Enter()
    {
        hasReachedDestination = false;
        hasPlayedAnimation = false;
        animationTimer = 0f;
        
        // Set patrol speed for investigating (walk speed)
        if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
        {
            enemy.Agent.speed = enemy.patrolSpeed;
            enemy.Agent.SetDestination(targetPosition);
        }
        else
        {
            Debug.LogWarning("DistractionState: NavMeshAgent is not available or not active");
        }
    }
    
    public override void Exit()
    {
        // Reset any animation triggers
        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Confused");
        }
        
        // Stop agent movement
        if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
        {
            enemy.Agent.ResetPath();
        }
    }
    
    public override void Perform()
    {
        // Safety check: ensure agent is available
        if (enemy.Agent == null || !enemy.Agent.isActiveAndEnabled || !enemy.Agent.isOnNavMesh)
        {
            // If agent is invalid, fall back to patrol
            stateMachine.ChangeState(new PatrolState());
            return;
        }
        
        // Check if we've reached the destination
        if (!hasReachedDestination)
        {
            // Check if player is visible while moving - can interrupt to attack immediately
            if (enemy.CanSeePlayer())
            {
                stateMachine.ChangeState(new AttackState());
                return;
            }
            
            float distanceToTarget = Vector3.Distance(enemy.transform.position, targetPosition);
            
            // Check if agent has reached the destination
            if (distanceToTarget <= DESTINATION_THRESHOLD || 
                (enemy.Agent.remainingDistance < DESTINATION_THRESHOLD && !enemy.Agent.pathPending))
            {
                hasReachedDestination = true;
                
                // Stop the agent
                enemy.Agent.ResetPath();
                
                // Play confused animation
                if (enemy.Animator != null)
                {
                    enemy.Animator.SetTrigger("Confused");
                    hasPlayedAnimation = true;
                    animationTimer = 0f;
                }
                else
                {
                    Debug.LogWarning("DistractionState: Animator is not available");
                    // If no animator, skip animation and transition immediately
                    hasPlayedAnimation = true;
                    animationTimer = ANIMATION_DURATION; // Skip wait time
                }
            }
            // If agent can't reach destination (path invalid), transition to patrol
            else if (!enemy.Agent.pathPending && enemy.Agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("DistractionState: Cannot reach target position, returning to patrol");
                stateMachine.ChangeState(new PatrolState());
            }
        }
        // Wait for animation to complete
        else if (hasPlayedAnimation)
        {
            animationTimer += Time.deltaTime;
            
            // Check if player is visible during animation - can interrupt to attack immediately
            if (enemy.CanSeePlayer())
            {
                stateMachine.ChangeState(new AttackState());
                return;
            }
            
            // Check if animation has completed (full 8 seconds)
            bool animationComplete = false;
            
            if (enemy.Animator != null)
            {
                AnimatorStateInfo stateInfo = enemy.Animator.GetCurrentAnimatorStateInfo(0);
                bool isInConfusedAnimation = stateInfo.IsName("Confused") || stateInfo.IsName("Confusion");
                
                // Animation is complete if:
                // 1. We're in the confused animation and it has fully played (normalizedTime >= 1)
                // 2. OR timer has reached the full 8 seconds (fallback)
                if (isInConfusedAnimation && stateInfo.normalizedTime >= 1f)
                {
                    animationComplete = true;
                }
                else if (animationTimer >= ANIMATION_DURATION)
                {
                    animationComplete = true;
                }
            }
            else
            {
                // No animator, just wait for full duration
                if (animationTimer >= ANIMATION_DURATION)
                {
                    animationComplete = true;
                }
            }
            
            // Only transition to patrol after full animation completes
            if (animationComplete)
            {
                // Animation complete, transition to patrol (player check already done above)
                stateMachine.ChangeState(new PatrolState());
            }
        }
    }
    
}

