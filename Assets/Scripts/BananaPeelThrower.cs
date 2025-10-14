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
        if (Input.GetButtonDown("Fire1"))
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

                // Play animation on enemy and freeze movement
                Animator enemyAnimator = hit.transform.GetComponentInChildren<Animator>();
                if (enemyAnimator != null)
                    StartCoroutine(PlayBananaHitAnimation(enemy.gameObject, enemyAnimator));

                // Deal damage
                enemy.TakeDamage(damage);

                // Throw peel to enemy’s feet
                Vector3 targetPosition = hit.transform.position;
                targetPosition.y = 0.09f;
                ThrowBananaPeel(targetPosition);
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

        // Add Rigidbody if it doesn't have one
        Rigidbody rb = bananaPeel.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bananaPeel.AddComponent<Rigidbody>();
        }

        // Simple throw: point from throw point to target with throw force
        Vector3 direction = (targetPosition - throwPoint.position).normalized;
        Vector3 velocity = direction * throwForce;

        // Add a bit of upward arc
        velocity.y += 5f;

        // Apply the velocity
        rb.linearVelocity = velocity;
    }

    public System.Collections.IEnumerator PlayBananaHitAnimation(GameObject enemy, Animator animator)
    {
        // Freeze enemy movement by disabling the movement script
        SimplePatrol patrolScript = enemy.GetComponent<SimplePatrol>();
        bool hadPatrolScript = patrolScript != null && patrolScript.enabled;

        if (hadPatrolScript)
        {
            patrolScript.enabled = false; // Disable movement
        }

        // Play the banana hit animation
        animator.SetTrigger(enemyAnimationTrigger);
        if (slipSound != null && slipAudioSource != null)
        {
            slipAudioSource.pitch = Random.Range(0.9f, 1.1f);
            slipAudioSource.PlayOneShot(slipSound);
        }

        // Wait for animation to complete


        // Wait for animation to complete
        yield return new WaitForSeconds(animationDuration);

        // Smoothly transition back to default animation using CrossFade
        animator.CrossFade(defaultAnimationState, transitionDuration);

        // Make sure these are reset
        animator.SetBool("IsLookingAround", false);
        animator.SetBool("IsAttacking", false);

        // Wait for the crossfade to complete
        yield return new WaitForSeconds(transitionDuration);

        // Re-enable movement
        if (hadPatrolScript && patrolScript != null)
        {
            patrolScript.enabled = true; // Re-enable movement
        }
    }
}
