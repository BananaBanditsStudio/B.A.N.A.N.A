using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityTutorial.PlayerControl;


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

    [Header("Player Reference")]
    [Tooltip("Manually assign the player GameObject here, or leave empty to find by tag automatically")]
    [SerializeField]
    private GameObject playerReference;
    
    private GameObject player;
    private Animator animator;
    public float sightDistance = 20f;
    public float fieldOfView = 85;
    
    private float playerSearchCooldown = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 1f; // Search every second if player is null
    private bool hasLoggedPlayerNotFoundWarning = false;

    [Header("Attack Behavior")]
    [Tooltip("The type of attack behavior to use. Change this to swap attack behaviors.")]
    public AttackBehaviorType attackBehaviorType = AttackBehaviorType.Throw;

    [Header("Ranged/Throw Attack Settings")]
    [Tooltip("Rate of fire for ranged attacks (seconds between shots)")]
    public float rangedFireRate = 2f;
    [Tooltip("Bullet/projectile prefab to throw")]
    public GameObject bulletPrefab;
    [Tooltip("Transform representing where projectiles spawn from")]
    public Transform gunBarrel;

    [Header("Melee Attack Settings")]
    [Tooltip("Range at which melee attacks can hit")]
    public float meleeAttackRange = 2f;
    [Tooltip("Cooldown between melee attacks (seconds)")]
    public float meleeAttackCooldown = 2f;
    [Tooltip("Damage dealt by melee attacks")]
    public float meleeDamage = 10f;
    [Tooltip("Delay before damage is applied after attack animation starts (seconds)")]
    public float meleeDamageDelay = 1.3f;

    [Header("Charge Attack Settings")]
    [Tooltip("Speed at which enemy charges towards player")]
    public float chargeSpeed = 15f;
    [Tooltip("Distance at which explosion triggers")]
    public float explosionRange = 2f;
    [Tooltip("Damage dealt to player on explosion")]
    public float explosionDamage = 50f;
    [Tooltip("Explosion prefab to spawn when using Charge attack behavior")]
    public GameObject explosionPrefab;

    [Header("Big Jump Attack Settings")]
    [Tooltip("Range at which big jump attacks can be initiated")]
    public float bigJumpRange = 8f;
    [Tooltip("Cooldown between big jump attacks (seconds)")]
    public float bigJumpCooldown = 5f;
    [Tooltip("Damage dealt by big jump landing")]
    public float bigJumpDamage = 25f;
    [Tooltip("Delay before damage is applied after jump starts (seconds)")]
    public float bigJumpDamageDelay = 1.5f;
    [Tooltip("Radius of AOE damage on landing")]
    public float bigJumpAOERadius = 4f;
    [Tooltip("Camera shake intensity on landing")]
    public float bigJumpShakeIntensity = 0.3f;
    [Tooltip("Camera shake duration (seconds)")]
    public float bigJumpShakeDuration = 0.5f;
    [Tooltip("Effect prefab to spawn on landing")]
    public GameObject bigJumpEffectPrefab;

    [Header("Big Melee Attack Settings")]
    [Tooltip("Range at which big melee attacks can hit")]
    public float bigMeleeAttackRange = 3f;
    [Tooltip("Cooldown between big melee attacks (seconds)")]
    public float bigMeleeAttackCooldown = 3f;
    [Tooltip("Damage dealt by big melee attacks")]
    public float bigMeleeDamage = 20f;
    [Tooltip("Delay before damage is applied after attack animation starts (seconds)")]
    public float bigMeleeDamageDelay = 1.5f;
    [Tooltip("Knockback force applied to player")]
    public float bigMeleeKnockback = 5f;

    [Header("Movement Speed")]
    [Tooltip("Walk speed for patrolling (matches blend tree: 0.1 = walk)")]
    public float patrolSpeed = 2f;
    [Tooltip("Run speed for chasing/attacking (matches blend tree: 3 = run)")]
    public float chaseSpeed = 6f;

    [Header("Sight Visualization")]
    public bool showSightCircle = true;
    private LineRenderer sightCircleRenderer;
    
    void Start()
    {
        stateMachine = GetComponent<StateMachine>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        
        // Try to find the player
        FindPlayer();
        
        if (agent != null)
        {
            agent.speed = patrolSpeed;
        }
        
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
        // Retry finding player if it's null (handles cases where player spawns after enemy)
        if (player == null)
        {
            playerSearchCooldown -= Time.deltaTime;
            if (playerSearchCooldown <= 0f)
            {
                FindPlayer();
                playerSearchCooldown = PLAYER_SEARCH_INTERVAL;
            }
        }
        
        CanSeePlayer();
        if (stateMachine != null && stateMachine.activeState != null)
        {
            currentState = stateMachine.activeState.ToString();
        }
        if (animator != null && agent != null)
        {
            animator.SetFloat("speed", agent.velocity.magnitude);
        }
        DrawSightCircle();
    }
    
    /// <summary>
    /// Finds the player GameObject using multiple methods:
    /// 1. Uses manually assigned playerReference if set
    /// 2. Searches for GameObject with "Player" tag
    /// 3. Searches for PlayerController component as fallback
    /// 4. Logs warning if player cannot be found (throttled by search interval)
    /// </summary>
    private void FindPlayer()
    {
        // First, try to use the manually assigned reference
        if (playerReference != null)
        {
            player = playerReference;
            hasLoggedPlayerNotFoundWarning = false; // Reset warning flag when player is found
            return;
        }
        
        // Try to find by tag
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            player = foundPlayer;
            hasLoggedPlayerNotFoundWarning = false; // Reset warning flag when player is found
            return;
        }
        
        // If still not found, try to find by component (PlayerController)
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.gameObject;
            Debug.Log($"EnemyWithSM on {gameObject.name}: Found player by PlayerController component");
            hasLoggedPlayerNotFoundWarning = false; // Reset warning flag when player is found
            return;
        }
        
        // Log warning if player still not found (only once to avoid spam)
        if (player == null && !hasLoggedPlayerNotFoundWarning)
        {
            Debug.LogWarning($"EnemyWithSM on {gameObject.name}: Could not find player! Make sure:" +
                           "\n1. Player GameObject has 'Player' tag, OR" +
                           "\n2. Player GameObject has PlayerController component, OR" +
                           "\n3. Manually assign playerReference in the inspector");
            hasLoggedPlayerNotFoundWarning = true;
        }
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
