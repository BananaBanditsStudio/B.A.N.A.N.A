using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public BaseState activeState;
    public PatrolState patrolState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        patrolState = new PatrolState();
        ChangeState(patrolState);
    }

    // Update is called once per frame
    void Update()
    {
        if (activeState != null)
        {
            activeState.Perform();
        }

    }

    public void Initialize()
    {

    }


    public void ChangeState(BaseState newState)
    {
        // Run the cleanup of the current state
        if (activeState != null)
        {
            activeState.Exit();
        }

        // Switch to the new state
        activeState = newState;

        // Enter into the new state
        if (activeState != null)
        {
            activeState.stateMachine = this;
            activeState.enemy = GetComponent<EnemyWithSM>();
            activeState.Enter();
        }
    }
}
