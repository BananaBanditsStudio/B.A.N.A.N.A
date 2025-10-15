// using UnityEngine;
// public class EnemySpin : MonoBehaviour
// {
//     public float degreesPerSecond = 30f;
//     void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
// }


using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SimplePatrol : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 2.5f;
    public float arriveDist = 0.15f;

    [Header("Look-around")]
    public bool lookAroundAtWaypoint = true;
    [Range(0f, 360f)] public float lookAngle = 60f;
    public int sweeps = 1;
    public float lookTurnSpeed = 180f;
    public float waitAtWaypoint = 1f;

    [Header("Animation")]
    public Animator animator;
    public bool IsAttacking = false;
    public bool IsRunning = false;
    
    [Header("Movement")]
    private CharacterController characterController;
    private NavMeshAgent navMeshAgent;
    private Vector3 lastMoveDirection = Vector3.zero;
    private float moveSmoothing = 10f;
    
    [Header("Navigation")]
    public bool useNavMesh = true;
    public float navMeshSpeed = 2.5f;
    public float navMeshAcceleration = 8f;
    public float navMeshAngularSpeed = 360f; // Increased for tighter turns
    public float navMeshStoppingDistance = 0.1f;
    public float rotationSpeed = 720f; // Manual rotation speed

    [Header("Chase Behavior")]
    public float chaseSpeed = 4f; // Base chase speed (walking)
    public float runningSpeed = 6f; // Speed when running animation is active
    public float chaseArriveDist = 1.5f;
    public float losePlayerTime = 3f; // How long to chase after losing sight

    [Header("Audio")]
    public AudioSource chaseAudio;
    public AudioClip chaseClip;
    public float fadeSpeed = 2f;
    public static bool isChaseAudioPlaying = false;


    [Header("Attack Behavior")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    private float lastAttackTime = 0f;
    public float damage = 10f;
    public GameObject player;
    private PlayerHealth playerHealth;

    [Header("Weapon Pickup")]
    public float weaponDetectionRange = 5f;
    public float weaponPickupRange = 1.5f;
    public LayerMask weaponLayerMask = -1;
    private GameObject targetWeapon = null;
    private bool isSeekingWeapon = false;
    private EnemyWeaponHandler weaponHandler;

    // Attack state tracking
    private bool isAttackInProgress = false;
    private Coroutine currentAttackCoroutine = null;

    int index = 0, dir = 1;
    public bool loop = true;

    // Chase state
    Transform currentTarget;
    float timeSinceLastSeen = 0f;
    bool isChasing = false;
    FieldOfView3D fieldOfView;

    void Start()
    {
        // Get CharacterController component
        characterController = GetComponent<CharacterController>();
        
        // Get or add NavMeshAgent component
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null && useNavMesh)
        {
            navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        // Configure NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = navMeshSpeed;
            navMeshAgent.acceleration = navMeshAcceleration;
            navMeshAgent.angularSpeed = navMeshAngularSpeed;
            navMeshAgent.stoppingDistance = navMeshStoppingDistance;
            navMeshAgent.radius = 0.3f; // Reduced radius for tighter movement
            navMeshAgent.autoBraking = true;
            navMeshAgent.updateRotation = false; // We'll handle rotation manually
            navMeshAgent.updateUpAxis = true;
            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance; // Better obstacle avoidance
        }
        
        // Disable root motion to prevent animation from affecting position
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Look for FieldOfView3D in children since it's on a child GameObject
        fieldOfView = GetComponentInChildren<FieldOfView3D>();
        if (fieldOfView != null)
        {
            fieldOfView.OnDetectionStateChanged += OnPlayerDetected;
        }
        else
        {
            Debug.LogWarning("FieldOfView3D component not found in children. Make sure FieldOfView3D script is attached to a child GameObject.");
        }

        playerHealth = player.GetComponent<PlayerHealth>();
        weaponHandler = GetComponent<EnemyWeaponHandler>();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Check if we need to seek a weapon first
        if (isSeekingWeapon && targetWeapon != null)
        {
            SeekWeapon();
        }
        // Check if we should be chasing
        else if (isChasing && currentTarget != null)
        {
            ChasePlayer();
        }
        else
        {
            // Check for nearby weapons if we don't have one
            if (weaponHandler != null && !weaponHandler.isHolding)
            {
                CheckForNearbyWeapons();
            }

            Patrol();
        }
    }

    void Patrol()
    {
        Vector3 tgt = waypoints[index].position;
        Vector3 pos = transform.position;
        Vector3 to = (tgt - pos); to.y = 0f;

        // Check if we've arrived at the waypoint
        bool hasArrived = (transform.position - tgt).sqrMagnitude <= arriveDist * arriveDist;

        // Only move if we haven't arrived yet and we're not busy (not in AtPoint coroutine)
        if (!hasArrived && !busy)
        {
            if (useNavMesh && navMeshAgent != null)
            {
                // Use NavMesh for pathfinding
                if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
                {
                    navMeshAgent.SetDestination(tgt);
                }
                
                // Face the waypoint direction
                SmoothLookAt(to);
                
                // Set animation to moving (not looking around)
                SetLookingAroundAnimation(false);
            }
            else
            {
                // Use direct movement (old system)
                if (to.sqrMagnitude > 0.0001f)
                {
                    Vector3 moveDirection = to.normalized;
                    MoveCharacter(moveDirection, moveSpeed);
                    
                    // Face movement direction
                    SmoothLookAt(to);
                    // Set animation to moving (not looking around)
                    SetLookingAroundAnimation(false);
                }
            }
        }

        // arrived?
        if (hasArrived && !busy)
            StartCoroutine(AtPoint());
    }

    void ChasePlayer()
    {
        if (currentTarget == null)
        {
            StopChasing();
            return;
        }

        Vector3 pos = transform.position;
        Vector3 to = (currentTarget.position - pos); to.y = 0f;

        // Check if we can still see the target
        if (fieldOfView != null && fieldOfView.visibleTargets.Contains(currentTarget))
        {
            timeSinceLastSeen = 0f;

            float distanceToTarget = to.magnitude;

            // Check if we're close enough to attack (only if we're actually chasing)
            if (distanceToTarget <= attackRange && Time.time - lastAttackTime >= attackCooldown && !isAttackInProgress && isChasing)
            {
                // Stop running and start attack
                IsRunning = false;
                UpdateRunningAnimation();
                
                IsAttacking = true;
                isAttackInProgress = true;
                UpdateAttackAnimation();
                lastAttackTime = Time.time;

                // Stop moving during attack
                // Start damage coroutine and store reference
                currentAttackCoroutine = StartCoroutine(DealDamageAfterDelay(1.3f));
            }
            else if (distanceToTarget > attackRange)
            {
                // Move towards target if not in attack range
                if (useNavMesh && navMeshAgent != null)
                {
                    // Use NavMesh for pathfinding to target
                    UpdateNavMeshDestination(currentTarget.position);
                    navMeshAgent.speed = IsRunning ? runningSpeed : chaseSpeed;
                    
                    // Face the direction the NavMesh agent is actually moving
                    if (navMeshAgent.velocity.sqrMagnitude > 0.1f)
                    {
                        ResponsiveLookAt(navMeshAgent.velocity);
                    }
                    else
                    {
                        // Fallback to facing target if not moving
                        ResponsiveLookAt(to);
                    }
                }
                else if (to.sqrMagnitude > 0.0001f)
                {
                    Vector3 moveDirection = to.normalized;
                    // Use running speed when running animation is active
                    float currentSpeed = IsRunning ? runningSpeed : chaseSpeed;
                    MoveCharacter(moveDirection, currentSpeed);
                    
                    // Face movement direction
                    SmoothLookAt(to);
                }

                // Set running animation when chasing (only if not already running)
                if (!IsRunning)
                {
                    IsRunning = true;
                    UpdateRunningAnimation();
                }

                // Only stop attacking if we were previously attacking but now need to move
                // This prevents interrupting an attack that just started
                if (isAttackInProgress && IsAttacking)
                {
                    // Only interrupt if the attack has been going for a while (not just started)
                    if (Time.time - lastAttackTime > 0.5f)
                    {
                        IsAttacking = false;
                        isAttackInProgress = false;
                        UpdateAttackAnimation();
                        
                        // Stop the damage coroutine if it's still running
                        if (currentAttackCoroutine != null)
                        {
                            StopCoroutine(currentAttackCoroutine);
                            currentAttackCoroutine = null;
                        }
                    }
                }
            }
            else if (isAttackInProgress && distanceToTarget > attackRange)
            {
                // Player moved out of range during attack - cancel the attack and resume running
                IsAttacking = false;
                isAttackInProgress = false;
                UpdateAttackAnimation();
                
                // Resume running animation (only if not already running)
                if (!IsRunning)
                {
                    IsRunning = true;
                    UpdateRunningAnimation();
                }
                
                // Stop the damage coroutine if it's still running
                if (currentAttackCoroutine != null)
                {
                    StopCoroutine(currentAttackCoroutine);
                    currentAttackCoroutine = null;
                }
            }

            // Face the target
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 720f * Time.deltaTime);
        }
        else
        {
            // Lost sight of target
            timeSinceLastSeen += Time.deltaTime;

            // Continue moving towards last known position for a short time
            if (timeSinceLastSeen <= losePlayerTime)
            {
                if (useNavMesh && navMeshAgent != null)
                {
                    // Use NavMesh for pathfinding to last known position
                    UpdateNavMeshDestination(currentTarget.position);
                    navMeshAgent.speed = IsRunning ? runningSpeed : chaseSpeed;
                    
                    // Face the direction the NavMesh agent is actually moving
                    if (navMeshAgent.velocity.sqrMagnitude > 0.1f)
                    {
                        ResponsiveLookAt(navMeshAgent.velocity);
                    }
                    else
                    {
                        // Fallback to facing last known position if not moving
                        ResponsiveLookAt(to);
                    }
                }
                else if (to.sqrMagnitude > 0.0001f)
                {
                    Vector3 moveDirection = to.normalized;
                    // Use running speed when running animation is active
                    float currentSpeed = IsRunning ? runningSpeed : chaseSpeed;
                    MoveCharacter(moveDirection, currentSpeed);
                    
                    // Face movement direction
                    SmoothLookAt(to);
                }

                // Keep running animation when moving towards last known position (only if not already running)
                if (!IsRunning)
                {
                    IsRunning = true;
                    UpdateRunningAnimation();
                }
            }
            else
            {
                // Lost the player, return to patrol
                if (isAttackInProgress)
                {
                    IsAttacking = false;
                    isAttackInProgress = false;
                    UpdateAttackAnimation();

                    // Stop the damage coroutine if it's still running
                    if (currentAttackCoroutine != null)
                    {
                        StopCoroutine(currentAttackCoroutine);
                        currentAttackCoroutine = null;
                    }
                }
                
                // Stop running animation when returning to patrol
                IsRunning = false;
                UpdateRunningAnimation();
                
                StopChasing();
            }
        }
    }

    bool busy;
    IEnumerator AtPoint()
    {
        if (busy) yield break;
        busy = true;

        // Set animation to idle/looking around immediately when reaching waypoint
        SetLookingAroundAnimation(true);

        if (waitAtWaypoint > 0f) yield return new WaitForSeconds(waitAtWaypoint);

        if (lookAroundAtWaypoint && lookAngle > 1f && sweeps > 0)
        {

            Quaternion baseRot = transform.rotation;
            Quaternion left = baseRot * Quaternion.Euler(0f, -lookAngle, 0f);
            Quaternion right = baseRot * Quaternion.Euler(0f, lookAngle, 0f);

            for (int i = 0; i < sweeps; i++)
            {
                yield return TurnTo(left);
                yield return new WaitForSeconds(0.1f);
                yield return TurnTo(right);
                yield return new WaitForSeconds(0.1f);
            }
            yield return TurnTo(baseRot);

            // Set animation back to moving
            SetLookingAroundAnimation(false);
        }

        if (loop) index = (index + 1) % waypoints.Length;
        else { if (index == 0) dir = 1; else if (index == waypoints.Length - 1) dir = -1; index += dir; }

        // Stop NavMesh agent when reaching waypoint
        if (useNavMesh && navMeshAgent != null)
        {
            navMeshAgent.ResetPath();
        }

        busy = false;
    }

    IEnumerator TurnTo(Quaternion q)
    {
        while (Quaternion.Angle(transform.rotation, q) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, lookTurnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = q;
    }

    void SetLookingAroundAnimation(bool isLookingAround)
    {
        if (animator != null)
        {
            animator.SetBool("IsLookingAround", isLookingAround);
            // Update attack animation based on current state
            UpdateAttackAnimation();
        }
    }

    void UpdateAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsAttacking", IsAttacking);
        }
    }

    void UpdateRunningAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsRunning", IsRunning);
        }
    }

    void MoveCharacter(Vector3 direction, float speed)
    {
        // Only move if we have a valid direction
        if (direction.magnitude < 0.1f) 
        {
            lastMoveDirection = Vector3.zero;
            return;
        }
        
        // Smooth the movement direction
        Vector3 targetDirection = direction.normalized;
        lastMoveDirection = Vector3.Lerp(lastMoveDirection, targetDirection, moveSmoothing * Time.deltaTime);
        
        Vector3 movement = lastMoveDirection * speed * Time.deltaTime;
        
        if (characterController != null)
        {
            // Use CharacterController for smooth movement
            characterController.Move(movement);
        }
        else
        {
            // Fallback to direct transform movement
            transform.position += movement;
        }
    }

    void SmoothLookAt(Vector3 targetDirection, float customRotationSpeed = -1f)
    {
        if (targetDirection.sqrMagnitude > 0.0001f)
        {
            float speed = customRotationSpeed > 0 ? customRotationSpeed : rotationSpeed;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed * Time.deltaTime);
        }
    }

    void ResponsiveLookAt(Vector3 targetDirection)
    {
        if (targetDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            float angle = Quaternion.Angle(transform.rotation, targetRotation);
            
            // Use faster rotation for sharp turns
            float speed = angle > 90f ? rotationSpeed * 1.5f : rotationSpeed;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed * Time.deltaTime);
        }
    }

    void UpdateNavMeshDestination(Vector3 destination)
    {
        if (navMeshAgent != null && useNavMesh)
        {
            // Only update destination if it's significantly different to avoid path recalculation
            float distance = Vector3.Distance(navMeshAgent.destination, destination);
            if (distance > 1f || !navMeshAgent.hasPath)
            {
                navMeshAgent.SetDestination(destination);
            }
        }
    }

    void OnPlayerDetected(bool isDetected)
    {
        if (isDetected && fieldOfView != null && fieldOfView.visibleTargets.Count > 0)
        {
            currentTarget = fieldOfView.visibleTargets[0];
            isChasing = true;
            timeSinceLastSeen = 0f;
            StopAllCoroutines();
            busy = false;
            SetLookingAroundAnimation(false);

            // Start chase sound only if not already playing
            if (!isChaseAudioPlaying && chaseAudio && chaseClip)
            {
                chaseAudio.clip = chaseClip;
                chaseAudio.Play();
                isChaseAudioPlaying = true;
            }
        }
        else if (!isDetected && isChasing)
        {
            timeSinceLastSeen = 0f;
        }
    }



    void StopChasing()
    {
        isChasing = false;
        currentTarget = null;
        timeSinceLastSeen = 0f;
        
        // Stop NavMesh agent if using NavMesh
        if (useNavMesh && navMeshAgent != null)
        {
            navMeshAgent.ResetPath();
        }
        
        // Stop running animation
        IsRunning = false;
        UpdateRunningAnimation();
        
        // Force stop any ongoing attack and reset all attack states
        IsAttacking = false;
        isAttackInProgress = false;
        UpdateAttackAnimation();
        
        // Stop the damage coroutine if it's still running
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            currentAttackCoroutine = null;
        }
        
        // Reset attack timer to prevent immediate re-attack
        lastAttackTime = Time.time;
        
        // Force reset all movement and animation states
        lastMoveDirection = Vector3.zero;

        // Stop chase sound (but only once)
        if (isChaseAudioPlaying && chaseAudio && chaseAudio.isPlaying)
        {
            StartCoroutine(FadeOut(chaseAudio, fadeSpeed));
            isChaseAudioPlaying = false;
        }
    }



    void OnDestroy()
    {
        if (fieldOfView != null)
        {
            fieldOfView.OnDetectionStateChanged -= OnPlayerDetected;
        }
    }


    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only deal damage if the attack is still in progress
        if (isAttackInProgress && playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // Reset attack state after damage is dealt
        IsAttacking = false;
        isAttackInProgress = false;
        UpdateAttackAnimation();
        currentAttackCoroutine = null;
    }

    IEnumerator FadeOut(AudioSource source, float fadeSpeed)
    {
        while (source.volume > 0.01f)
        {
            source.volume -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        source.Stop();
        source.volume = 1f;
    }

    void CheckForNearbyWeapons()
    {
        // Find all colliders within weapon detection range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, weaponDetectionRange, weaponLayerMask);

        GameObject closestWeapon = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            // Check if this is a weapon (has Rigidbody and is not kinematic)
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestWeapon = col.gameObject;
                    closestDistance = distance;
                }
            }
        }

        if (closestWeapon != null)
        {
            targetWeapon = closestWeapon;
            isSeekingWeapon = true;
        }
    }

    void SeekWeapon()
    {
        if (targetWeapon == null)
        {
            isSeekingWeapon = false;
            return;
        }

        Vector3 pos = transform.position;
        Vector3 to = (targetWeapon.transform.position - pos); to.y = 0f;
        float distanceToWeapon = to.magnitude;

        // Check if weapon is still valid (exists and not picked up by someone else)
        if (targetWeapon == null || targetWeapon.GetComponent<Rigidbody>() == null || targetWeapon.GetComponent<Rigidbody>().isKinematic)
        {
            targetWeapon = null;
            isSeekingWeapon = false;
            return;
        }

        // If we're close enough, pick up the weapon
        if (distanceToWeapon <= weaponPickupRange)
        {
            PickupWeapon();
        }
        else
        {
            // Move towards the weapon
            if (useNavMesh && navMeshAgent != null)
            {
                // Use NavMesh for pathfinding to weapon
                UpdateNavMeshDestination(targetWeapon.transform.position);
                navMeshAgent.speed = moveSpeed;
                
                // Face the direction the NavMesh agent is actually moving
                if (navMeshAgent.velocity.sqrMagnitude > 0.1f)
                {
                    ResponsiveLookAt(navMeshAgent.velocity);
                }
                else
                {
                    // Fallback to facing weapon if not moving
                    ResponsiveLookAt(to);
                }
            }
            else if (to.sqrMagnitude > 0.0001f)
            {
                Vector3 moveDirection = to.normalized;
                MoveCharacter(moveDirection, moveSpeed);
                
                // Face the weapon
                SmoothLookAt(to);
            }
        }
    }

    void PickupWeapon()
    {
        if (weaponHandler != null && targetWeapon != null)
        {
            // Set the weapon as the bat and equip it
            weaponHandler.bat = targetWeapon;
            weaponHandler.EquipBat();

            // Clear target
            targetWeapon = null;
            isSeekingWeapon = false;
        }
    }

}
