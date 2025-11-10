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
        // Safety check: agent must be active and on NavMesh
        if (enemy.Agent == null || !enemy.Agent.isActiveAndEnabled || !enemy.Agent.isOnNavMesh)
            return;
        
        if (enemy.Agent.remainingDistance < 0.2f)
        {
            waypointIndex = (waypointIndex + 1) % enemy.path.waypoints.Count;
            enemy.Agent.SetDestination(enemy.path.waypoints[waypointIndex].position);
        }

    }
}