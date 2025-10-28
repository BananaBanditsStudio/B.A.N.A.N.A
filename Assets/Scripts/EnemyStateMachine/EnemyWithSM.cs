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

    private GameObject player;
    public float sightDistance = 20f;
    public float fieldOfView = 85;
    void Start()
    {
        stateMachine = GetComponent<StateMachine>();
        agent = GetComponent<NavMeshAgent>();
        stateMachine.Initialize();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        CanSeePlayer();
    }

    public bool CanSeePlayer(){
        if (player != null) {
            if (Vector3.Distance(transform.position, player.transform.position) <= sightDistance) {
                Vector3 direction = (player.transform.position - transform.position);
                float angleToPlayer = Vector3.Angle(direction, transform.forward);
                if (angleToPlayer >= -fieldOfView && angleToPlayer <= fieldOfView) {
                    Ray ray = new Ray(transform.position, direction);
                    Debug.DrawRay(ray.origin, ray.direction * sightDistance, Color.red, 0.1f);
                }
            }
            
        }

        return true;
    }
}
