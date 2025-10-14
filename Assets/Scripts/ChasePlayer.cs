using UnityEngine;
using UnityEngine.AI;

public class ChasePlayer : MonoBehaviour
{
    [Header("Chase Settings")]
    public Transform player;
    public float chaseSpeed = 4f;
    public string playerTag = "Player"; // Fallback tag lookup if player not assigned
    
    [Header("Attack Behavior")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public float damage = 10f;
    public bool IsAttacking = false;
    
    [Header("Audio")]
    public AudioSource chaseAudio;
    public AudioClip chaseClip;
    public float fadeSpeed = 2f;
    public static bool isChaseAudioPlaying = false;
    
    [Header("Animation")]
    public Animator animator;
    public bool IsRunning = false;
    
    [Header("Movement")]
    private CharacterController characterController;
    private Vector3 lastMoveDirection = Vector3.zero;
    private float moveSmoothing = 10f;
    
    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private float lastAttackTime = 0f;
    private bool isAttackInProgress = false;
    private Coroutine currentAttackCoroutine = null;
    
    void Start()
    {
        // Get components
        characterController = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        
        // If player is not assigned on the prefab, try to find it by tag at runtime
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindWithTag(playerTag);
            if (found != null)
            {
                player = found.transform;
                playerHealth = found.GetComponent<PlayerHealth>();
            }
        }
        else if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        
        // Configure NavMeshAgent
        if (agent != null)
        {
            agent.updatePosition = false; // We move with CharacterController
            agent.updateRotation = false; // We rotate manually
            agent.speed = chaseSpeed;
            agent.acceleration = 12f;
            agent.angularSpeed = 720f;
            agent.stoppingDistance = attackRange;
        }
        
        // Disable root motion to prevent animation from affecting position
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }
    
    void Update()
    {
        if (player == null || agent == null) return;
        
        // Set destination to player
        agent.SetDestination(player.position);
        
        // Check if we can attack - only if we have a valid path to the player
        bool canReachPlayer = agent.hasPath && !agent.pathPending && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete;
        float distanceToPlayer = canReachPlayer ? agent.remainingDistance : Vector3.Distance(transform.position, player.position);
        float directDistance = Vector3.Distance(transform.position, player.position);
        
        // Check if we can attack - only if we can actually reach the player AND direct distance is reasonable
        if (canReachPlayer && distanceToPlayer <= attackRange && directDistance <= attackRange * 1.5f && Time.time - lastAttackTime >= attackCooldown && !isAttackInProgress)
        {
            // Stop and attack
            IsRunning = false;
            UpdateRunningAnimation();
            
            IsAttacking = true;
            isAttackInProgress = true;
            UpdateAttackAnimation();
            lastAttackTime = Time.time;
            
            // Deal damage after delay
            currentAttackCoroutine = StartCoroutine(DealDamageAfterDelay(1.3f));
        }
        else if (distanceToPlayer > attackRange || directDistance > attackRange * 1.5f)
        {
            // Get movement direction from NavMeshAgent
            Vector3 moveDirection = Vector3.zero;
            if (agent.hasPath && agent.remainingDistance > 0.1f)
            {
                moveDirection = agent.desiredVelocity.normalized;
            }
            
            // Move towards player
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                MoveCharacter(moveDirection, chaseSpeed);
                
                // Face movement direction
                Vector3 lookDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);
                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookDirection), 720f * Time.deltaTime);
                }
                
                // Set running animation
                if (!IsRunning)
                {
                    IsRunning = true;
                    UpdateRunningAnimation();
                }
            }
            
            // Stop attacking if we were attacking but now need to move
            if (isAttackInProgress && IsAttacking)
            {
                if (Time.time - lastAttackTime > 0.5f)
                {
                    IsAttacking = false;
                    isAttackInProgress = false;
                    UpdateAttackAnimation();
                    
                    if (currentAttackCoroutine != null)
                    {
                        StopCoroutine(currentAttackCoroutine);
                        currentAttackCoroutine = null;
                    }
                }
            }
        }
        else
        {
            // Stop running when close but not attacking
            if (IsRunning)
            {
                IsRunning = false;
                UpdateRunningAnimation();
            }
        }
        
        // Start chase sound if not already playing
        if (!isChaseAudioPlaying && chaseAudio && chaseClip && !chaseAudio.isPlaying)
        {
            chaseAudio.clip = chaseClip;
            chaseAudio.Play();
            isChaseAudioPlaying = true;
        }
        
        // Keep agent synced with our position
        agent.nextPosition = transform.position;
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
    
    void UpdateRunningAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsRunning", IsRunning);
        }
    }
    
    void UpdateAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsAttacking", IsAttacking);
        }
    }
    
    private System.Collections.IEnumerator DealDamageAfterDelay(float delay)
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
    
    void OnDestroy()
    {
        // Stop chase sound when destroyed
        if (isChaseAudioPlaying && chaseAudio && chaseAudio.isPlaying)
        {
            StartCoroutine(FadeOut(chaseAudio, fadeSpeed));
            isChaseAudioPlaying = false;
        }
    }
    
    System.Collections.IEnumerator FadeOut(AudioSource source, float fadeSpeed)
    {
        while (source.volume > 0.01f)
        {
            source.volume -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        source.Stop();
        source.volume = 1f;
    }
}
