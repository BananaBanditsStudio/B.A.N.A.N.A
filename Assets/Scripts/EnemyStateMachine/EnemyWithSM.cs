using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class EnemyWithSM : MonoBehaviour
{

    private StateMachine stateMachine;
    public StateMachine StateMachine { get { return stateMachine; } }
    private NavMeshAgent agent;
    public NavMeshAgent Agent { get { return agent; } }
    public GameObject Player { get { return player; } }
    public Animator Animator { get { return animator; } }
    public Path2 path;

    [SerializeField]
    private string currentState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private GameObject player;
    private Animator animator;
    public float sightDistance = 20f;
    public float fieldOfView = 85;
    public float fireRate = 2f;

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform gunBarrel;

    [Header("Sight Visualization")]
    public bool showSightCircle = true;
    private LineRenderer sightCircleRenderer;
    
    void Start()
    {
        stateMachine = GetComponent<StateMachine>();
        agent = GetComponent<NavMeshAgent>();
        stateMachine.Initialize();
        player = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponentInChildren<Animator>();
        
        // Setup LineRenderer for sight circle
        if (showSightCircle)
        {
            sightCircleRenderer = gameObject.AddComponent<LineRenderer>();
            
            // Use built-in default line material
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader != null)
            {
                sightCircleRenderer.material = new Material(shader);
                sightCircleRenderer.material.color = Color.yellow;
            }
            else
            {
                // Fallback: create a basic material
                Material fallbackMaterial = new Material(Shader.Find("Standard"));
                fallbackMaterial.color = Color.yellow;
                fallbackMaterial.SetFloat("_Metallic", 0f);
                fallbackMaterial.SetFloat("_Glossiness", 0f);
                sightCircleRenderer.material = fallbackMaterial;
            }
            
            sightCircleRenderer.startWidth = 0.1f;
            sightCircleRenderer.endWidth = 0.1f;
            sightCircleRenderer.useWorldSpace = true;
            sightCircleRenderer.loop = true;
            sightCircleRenderer.enabled = true;
            sightCircleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sightCircleRenderer.receiveShadows = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        CanSeePlayer();
        currentState = stateMachine.activeState.ToString();
        // Debug.Log("Speed: " + agent.velocity.magnitude);
        animator.SetFloat("speed", agent.velocity.magnitude);
        
        DrawSightCircle();
    }

    void DrawSightCircle()
    {
        if (!showSightCircle || sightCircleRenderer == null) return;
        
        // Cast a ray down to find actual ground level
        RaycastHit hit;
        float groundY = transform.position.y - 1f; // Default fallback
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f))
        {
            groundY = hit.point.y;
        }
        
        Vector3 center = new Vector3(transform.position.x, groundY + 0.05f, transform.position.z);
        int segments = 64;
        float angleStep = 360f / segments;
        
        // Set LineRenderer positions
        sightCircleRenderer.positionCount = segments + 1;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(
                Mathf.Cos(angle) * sightDistance,
                0f,
                Mathf.Sin(angle) * sightDistance
            );
            sightCircleRenderer.SetPosition(i, point);
        }
    }

    public bool CanSeePlayer(){
        if (player != null) {
            if (Vector3.Distance(transform.position, player.transform.position) <= sightDistance) {
                Vector3 direction = (player.transform.position - transform.position);
                float angleToPlayer = Vector3.Angle(direction, transform.forward);
                if (angleToPlayer >= -fieldOfView && angleToPlayer <= fieldOfView) {
                    Ray ray = new Ray(transform.position, direction);
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(ray, out hit, sightDistance)) {
                        if (hit.transform.gameObject == player) {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Triggers the enemy to investigate a distraction at the specified position.
    /// The enemy will move to the target location and play a confused animation upon arrival.
    /// </summary>
    /// <param name="targetPosition">The world position to investigate</param>
    public void TriggerDistraction(Vector3 targetPosition)
    {
        if (stateMachine != null)
        {
            stateMachine.ChangeState(new DistractionState(targetPosition));
        }
        else
        {
            Debug.LogWarning("EnemyWithSM: Cannot trigger distraction - StateMachine is null");
        }
    }

    /// <summary>
    /// Triggers the enemy to investigate a distraction at a GameObject's position.
    /// Useful when you want the enemy to check out a specific object.
    /// </summary>
    /// <param name="targetObject">The GameObject to investigate</param>
    public void TriggerDistraction(GameObject targetObject)
    {
        if (targetObject != null)
        {
            TriggerDistraction(targetObject.transform.position);
        }
        else
        {
            Debug.LogWarning("EnemyWithSM: Cannot trigger distraction - target GameObject is null");
        }
    }

}
