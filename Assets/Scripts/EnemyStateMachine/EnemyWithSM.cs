using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class EnemyWithSM : MonoBehaviour
{

    private StateMachine stateMachine;
    private NavMeshAgent agent;
    public NavMeshAgent Agent { get { return agent; } }
    public Path2 path;

    [SerializeField]
    private string currentState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stateMachine = GetComponent<StateMachine>();
        agent = GetComponent<NavMeshAgent>();
        stateMachine.Initialize();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
