public class PatrolState : BaseState
{
    public int waypointIndex;
    public override void Enter()
    {
    }

    public override void Exit()
    {
    }

    public override void Perform()
    {
        // Code to execute while in the patrol state
        PatrolCycle();
    }

    public void PatrolCycle()
    {
        if (enemy.Agent.remainingDistance < 0.2f)
        {
            waypointIndex = (waypointIndex + 1) % enemy.path.waypoints.Count;
            enemy.Agent.SetDestination(enemy.path.waypoints[waypointIndex].position);
        }

    }
}