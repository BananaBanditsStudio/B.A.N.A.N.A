// using UnityEngine;
// public class EnemySpin : MonoBehaviour
// {
//     public float degreesPerSecond = 30f;
//     void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
// }


using System.Collections;
using UnityEngine;

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

    [Header("Chase Behavior")]
    public float chaseSpeed = 4f;
    public float chaseArriveDist = 1.5f;
    public float losePlayerTime = 3f; // How long to chase after losing sight

    [Header("Audio")]
    public AudioSource chaseAudio;
    public AudioClip chaseClip;
    public float fadeSpeed = 2f;


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
            // move
            Vector3 step = to.normalized * moveSpeed * Time.deltaTime;
            if (step.sqrMagnitude >= to.sqrMagnitude) transform.position = tgt;
            else transform.position += step;

            // face movement
            if (to.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 720f * Time.deltaTime);
                // Set animation to moving (not looking around)
                SetLookingAroundAnimation(false);
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

            // Check if we're close enough to attack
            if (distanceToTarget <= attackRange && Time.time - lastAttackTime >= attackCooldown && !isAttackInProgress)
            {
                // Start attack
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
                Vector3 step = to.normalized * chaseSpeed * Time.deltaTime;
                transform.position += step;

                // Stop attacking when moving and cancel damage if attack was interrupted
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
            }
            else if (isAttackInProgress && distanceToTarget > attackRange)
            {
                // Player moved out of range during attack - cancel the attack
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
                Vector3 step = to.normalized * chaseSpeed * Time.deltaTime;
                transform.position += step;

                if (to.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 720f * Time.deltaTime);
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

            //  start chase sound
            if (chaseAudio && chaseClip)
            {
                chaseAudio.clip = chaseClip;
                if (!chaseAudio.isPlaying)
                    chaseAudio.Play();
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
        
        // Stop any ongoing attack
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

        //  stop chase sound
        if (chaseAudio && chaseAudio.isPlaying)
            StartCoroutine(FadeOut(chaseAudio, fadeSpeed));

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
        isAttackInProgress = false;
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
            Vector3 step = to.normalized * moveSpeed * Time.deltaTime;
            if (step.sqrMagnitude >= to.sqrMagnitude) 
                transform.position = targetWeapon.transform.position;
            else 
                transform.position += step;

            // Face the weapon
            if (to.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 720f * Time.deltaTime);
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
