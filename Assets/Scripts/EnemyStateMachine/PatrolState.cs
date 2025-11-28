using UnityEngine;

public class PatrolState : BaseState
{
    public int waypointIndex;
    public override void Enter()
    {
        if (enemy.Agent != null)
        {
            enemy.Agent.speed = enemy.patrolSpeed;
            enemy.Agent.isStopped = false;
        }
    }

    public override void Exit()
    {
    }

    public override void Perform()
    {
        // Code to execute while in the patrol state
        PatrolCycle();
        if (enemy.CanSeePlayer()) {
            stateMachine.ChangeState(new AttackState());
        }
    }

    public void PatrolCycle()
    {
        // 1. Safety Checks
        if (enemy.Agent == null || !enemy.Agent.isActiveAndEnabled || !enemy.Agent.isOnNavMesh)
            return;

        // 2. STOP if the agent is still calculating a path (Fixes the skipping bug)
        if (enemy.Agent.pathPending) 
            return;

        // ---------------------------------------------------------
        // 3. THE "SLOW TURN" LOGIC
        // ---------------------------------------------------------
        
        // Get the immediate direction the agent WANTS to go
        Vector3 nextCorner = enemy.Agent.steeringTarget;
        Vector3 targetDir = nextCorner - enemy.transform.position;
        targetDir.y = 0; // Flatten it so he doesn't look at the floor/sky

        if (targetDir != Vector3.zero)
        {
            // Calculate rotation
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            
            // Check angle between forward vector and target direction
            float angle = Vector3.Angle(enemy.transform.forward, targetDir);

            if (angle > 10f) // If angle is large (> 10 degrees)
            {
                // STOP MOVING
                enemy.Agent.isStopped = true; 
                
                // ROTATE SLOWLY
                // Adjust '5f' to change turn speed
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRot, 5f * Time.deltaTime);
                
                // Return here so we don't process arrival logic while turning
                return; 
            }
            else
            {
                // Angle is small, we are facing the right way. GO!
                enemy.Agent.isStopped = false;
                
                // Keep rotation synced nicely while walking
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRot, 10f * Time.deltaTime);
            }
        }

        // ---------------------------------------------------------
        // 4. ARRIVAL LOGIC
        // ---------------------------------------------------------

        // Check distance (bumped to 0.5f for better reliability)
        if (enemy.Agent.remainingDistance <= 0.5f)
        {
            waypointIndex = (waypointIndex + 1) % enemy.path.waypoints.Count;
            enemy.Agent.SetDestination(enemy.path.waypoints[waypointIndex].position);
        }
    }
}