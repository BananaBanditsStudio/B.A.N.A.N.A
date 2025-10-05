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
    [Range(0f,180f)] public float lookAngle = 60f;
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
    
    [Header("Attack Behavior")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    private float lastAttackTime = 0f;

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
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Check if we should be chasing
        if (isChasing && currentTarget != null)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        Vector3 tgt = waypoints[index].position;
        Vector3 pos = transform.position;
        Vector3 to = (tgt - pos); to.y = 0f;

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

        // arrived?
        if ((transform.position - tgt).sqrMagnitude <= arriveDist * arriveDist)
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
            if (distanceToTarget <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                // Start attack
                IsAttacking = true;
                UpdateAttackAnimation();
                lastAttackTime = Time.time;
                
                // Stop moving during attack
                // You can add attack logic here (damage, etc.)
            }
            else if (distanceToTarget > attackRange)
            {
                // Move towards target if not in attack range
                Vector3 step = to.normalized * chaseSpeed * Time.deltaTime;
                transform.position += step;
                
                // Stop attacking when moving
                IsAttacking = false;
                UpdateAttackAnimation();
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
                IsAttacking = false;
                UpdateAttackAnimation();
                StopChasing();
            }
        }
    }

    bool busy;
    IEnumerator AtPoint()
    {
        if (busy) yield break;
        busy = true;

        if (waitAtWaypoint > 0f) yield return new WaitForSeconds(waitAtWaypoint);

        if (lookAroundAtWaypoint && lookAngle > 1f && sweeps > 0)
        {
            // Set animation to idle/looking around
            SetLookingAroundAnimation(true);
            
            Quaternion baseRot = transform.rotation;
            Quaternion left = baseRot * Quaternion.Euler(0f, -lookAngle, 0f);
            Quaternion right = baseRot * Quaternion.Euler(0f,  lookAngle, 0f);

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
            // Start chasing the first visible target
            currentTarget = fieldOfView.visibleTargets[0];
            isChasing = true;
            timeSinceLastSeen = 0f; 
            
            // Stop any current patrol behavior
            StopAllCoroutines();
            busy = false;
            
            // Ensure we're not in looking around animation when chasing
            SetLookingAroundAnimation(false);
        }
        else if (!isDetected && isChasing)
        {
            // Player lost, start countdown to stop chasing
            timeSinceLastSeen = 0f;
        }
    }

    void StopChasing()
    {
        isChasing = false;
        currentTarget = null;
        timeSinceLastSeen = 0f;
        
        // Stop attacking when returning to patrol
        IsAttacking = false;
        UpdateAttackAnimation();
        
        // Resume patrol from current position
        // The patrol will continue from wherever we are
    }

    void OnDestroy()
    {
        if (fieldOfView != null)
        {
            fieldOfView.OnDetectionStateChanged -= OnPlayerDetected;
        }
    }
}
