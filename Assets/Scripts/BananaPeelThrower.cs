using UnityEngine;

public class BananaPeelThrower : MonoBehaviour
{
    [Header("Banana Peel Settings")]
    public float damage = 5f;
    public float range = 50f;
    public float throwForce = 10f; // Simple throw force

    [Header("References")]
    public Camera fpsCam;
    public Transform throwPoint; // Point from where the banana peel is thrown
    public GameObject bananaPeelPrefab; // Banana peel to spawn on enemy
    public GameObject throwEffect; // Particle effect when shooting

    [Header("Hit Sound")]
    public AudioClip bananaHitSound; // sound to play when you hit an enemy
    public AudioSource audioSource; // can be same one or separate

    public AudioSource m_shootingSound;

    [Header("Slip Sound")]
    public AudioClip slipSound; // the sound that plays when enemy slips
    public AudioSource slipAudioSource; // can reuse existing one or be separate




    [Header("Animation")]
    public string enemyAnimationTrigger = "Slip"; // Animation trigger name
    public string defaultAnimationState = "Walk"; // The animation to return to (Walk, Idle, etc.)
    public float transitionDuration = 0.5f; // How long to blend back to normal animation
    public float animationDuration = 4.5f; // Duration of the slip animation

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (slipAudioSource == null)
            slipAudioSource = GetComponent<AudioSource>();
    }


    void Update()
    {
        // Check if input is allowed (not paused or game over)
        if (Input.GetButtonDown("Fire1") && GameStateManager.CanShootStatic())
        {
            m_shootingSound.Play();
            Shoot();
        }
    }

    void Shoot()
    {
        // Raycast from gun to detect enemy hit
        RaycastHit hit;

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            // Check if we hit an enemy
            EnemyDamage enemy = hit.transform.GetComponent<EnemyDamage>();
            if (enemy != null)
            {
                // ✅ Play banana hit sound
                if (bananaHitSound != null && audioSource != null)
                    audioSource.PlayOneShot(bananaHitSound);

                // Play throw effect
                if (throwEffect != null)
                    Instantiate(throwEffect, throwPoint.position, throwPoint.rotation);

                // Drop weapon if enemy has one
                EnemyWeaponHandler weaponHandler = hit.transform.GetComponent<EnemyWeaponHandler>();
                if (weaponHandler != null)
                    weaponHandler.DropBat();

                // Resolve animator and target position now
                Animator enemyAnimator = hit.transform.GetComponentInChildren<Animator>();
                Vector3 targetPosition = hit.transform.position;
                // Adjust height based on enemy scale (some monkeys are 3x scale)
                float enemyScale = hit.transform.localScale.y;
                targetPosition.y -= 0.8f * enemyScale;

                if (enemy.IsSlipping() || enemy.health <= 0)
                {
                    // If already slipping or dead, just deal damage
                    enemy.TakeDamage(damage);
                }
                else
                {
                    // Mark slipping, then start animation and damage ON ARRIVAL of peel for sync
                    enemy.SetSlippingState(true);
                    ThrowBananaPeelWithArrivalCallback(targetPosition, () => {
                        if (enemy != null && enemyAnimator != null)
                        {
                            StartCoroutine(PlayBananaHitAnimation(enemy.gameObject, enemyAnimator));
                            enemy.TakeDamage(damage);
                        }
                    });
                }
                
                // If already slipping/dead, still throw the peel for visuals
                if (enemy.IsSlipping() || enemy.health <= 0)
                {
                    ThrowBananaPeel(targetPosition);
                }
            }


            // If we didn't hit an enemy, do nothing (don't throw projectile)
        }
    }

    void ThrowBananaPeel(Vector3 targetPosition)
    {
        if (bananaPeelPrefab == null || throwPoint == null)
        {
            Debug.LogWarning("Banana peel prefab or throw point not assigned!");
            return;
        }

        // Create the banana peel at throw point
        GameObject bananaPeel = Instantiate(bananaPeelPrefab, throwPoint.position, Quaternion.identity);

        // Add manual throw script instead of rigidbody
        BananaPeelThrow throwScript = bananaPeel.GetComponent<BananaPeelThrow>();
        if (throwScript == null)
        {
            throwScript = bananaPeel.AddComponent<BananaPeelThrow>();
        }

        // Configure the throw
        throwScript.InitializeThrow(throwPoint.position, targetPosition, throwForce);
    }

    // Throws peel and invokes callback exactly when the peel arrives
    void ThrowBananaPeelWithArrivalCallback(Vector3 targetPosition, System.Action onArrived)
    {
        if (bananaPeelPrefab == null || throwPoint == null)
        {
            Debug.LogWarning("Banana peel prefab or throw point not assigned!");
            return;
        }

        GameObject bananaPeel = Instantiate(bananaPeelPrefab, throwPoint.position, Quaternion.identity);

        BananaPeelThrow throwScript = bananaPeel.GetComponent<BananaPeelThrow>();
        if (throwScript == null)
        {
            throwScript = bananaPeel.AddComponent<BananaPeelThrow>();
        }

        throwScript.onArrived = onArrived;
        throwScript.InitializeThrow(throwPoint.position, targetPosition, throwForce);
    }

    public System.Collections.IEnumerator PlayBananaHitAnimation(GameObject enemy, Animator animator)
    {
        // Get enemy damage component to track slipping state
        EnemyDamage enemyDamage = enemy.GetComponent<EnemyDamage>();
        
        // Freeze enemy movement by disabling the movement script
        SimplePatrol patrolScript = enemy.GetComponent<SimplePatrol>();
        bool hadPatrolScript = patrolScript != null && patrolScript.enabled;

        if (hadPatrolScript)
        {
            patrolScript.enabled = false; // Disable movement
        }
        
        // Disable NavMeshAgent if present to prevent movement conflicts
        UnityEngine.AI.NavMeshAgent navAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        bool hadNavAgent = navAgent != null && navAgent.enabled;
        if (hadNavAgent)
        {
            navAgent.enabled = false;
        }
        
        // Disable CharacterController to allow animation to move the collider
        CharacterController characterController = enemy.GetComponent<CharacterController>();
        bool hadCharacterController = characterController != null && characterController.enabled;
        if (hadCharacterController)
        {
            characterController.enabled = false;
        }
        
        // Enable animation collider sync if present
        AnimationColliderSync colliderSync = enemy.GetComponent<AnimationColliderSync>();
        if (colliderSync == null)
        {
            colliderSync = enemy.AddComponent<AnimationColliderSync>();
        }
        colliderSync.enabled = true;

        // Play the banana hit animation
        animator.SetTrigger(enemyAnimationTrigger);
        if (slipSound != null && slipAudioSource != null)
        {
            slipAudioSource.pitch = Random.Range(0.9f, 1.1f);
            slipAudioSource.PlayOneShot(slipSound);
        }

        // Wait for animation to complete
        yield return new WaitForSeconds(animationDuration);

        // Check if enemy is still alive and not dead
        if (enemyDamage != null && enemyDamage.health > 0)
        {
            // Smoothly transition back to default animation using CrossFade
            animator.CrossFade(defaultAnimationState, transitionDuration);

            // Make sure these are reset
            animator.SetBool("IsLookingAround", false);
            animator.SetBool("IsAttacking", false);

            // Wait for the crossfade to complete
            yield return new WaitForSeconds(transitionDuration);

            // Re-enable movement components only if enemy is still alive
            if (enemyDamage.health > 0)
            {
                // Check if this enemy uses EnemyWithSM (state machine system)
                EnemyWithSM enemyWithSM = enemy.GetComponent<EnemyWithSM>();
                if (enemyWithSM != null)
                {
                    if (hadNavAgent && navAgent != null)
                    {
                        navAgent.enabled = true;
                    }
                }
                else
                {
                    // For SimplePatrol enemies, re-enable movement components
                    if (hadPatrolScript && patrolScript != null)
                    {
                        patrolScript.enabled = true; // Re-enable movement
                    }
                    
                    if (hadNavAgent && navAgent != null)
                    {
                        navAgent.enabled = true; // Re-enable NavMeshAgent
                    }
                    
                    if (hadCharacterController && characterController != null)
                    {
                        characterController.enabled = true; // Re-enable CharacterController
                    }
                }
            }
        }
        
        // Reset slipping state
        if (enemyDamage != null)
        {
            enemyDamage.SetSlippingState(false);
        }
        
        // Disable animation collider sync
        if (colliderSync != null)
        {
            colliderSync.enabled = false;
        }
    }
}
