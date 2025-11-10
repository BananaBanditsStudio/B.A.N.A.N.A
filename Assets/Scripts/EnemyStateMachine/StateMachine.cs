using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public BaseState activeState;
    
    void Start()
    {
        if (activeState == null)
        {
            ChangeState(new PatrolState());
        }
    }

    void Update()
    {
        if (activeState != null)
        {
            activeState.Perform();
        }
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
