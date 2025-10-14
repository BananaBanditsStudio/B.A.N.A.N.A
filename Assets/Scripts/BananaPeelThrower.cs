using UnityEngine;

public class BananaPeelThrower : MonoBehaviour
{
    [Header("Banana Peel Settings")]
    public float damage = 5f;
    public float range = 50f;

    [Header("References")]
    public Camera fpsCam;
    public GameObject bananaPeelPrefab; // Banana peel to spawn on enemy
    public GameObject throwEffect; // Particle effect when shooting
    public AudioSource m_shootingSound;

    [Header("Animation")]
    public string enemyAnimationTrigger = "Slip"; // Animation trigger name
    public string defaultAnimationState = "Walk"; // The animation to return to (Walk, Idle, etc.)
    public float transitionDuration = 0.5f; // How long to blend back to normal animation
    public float animationDuration = 4.5f; // Duration of the slip animation

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
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
        // Play throw effect
        if (throwEffect != null)
        {
            Instantiate(throwEffect, transform.position, transform.rotation);
        }

        // Raycast to detect hit
        RaycastHit hit;

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            // Check if we hit an enemy
            EnemyDamage enemy = hit.transform.GetComponent<EnemyDamage>();
            if (enemy != null)
            {
                // Spawn banana peel on the enemy
                if (bananaPeelPrefab != null)
                {
                    // Spawn banana peel at feet level (adjust y position)
                    Vector3 spawnPosition = hit.transform.position;
                    spawnPosition.y = 0.09f;
                    Instantiate(bananaPeelPrefab, spawnPosition, Quaternion.LookRotation(hit.normal));
                }

                // Drop weapon if enemy has one
                EnemyWeaponHandler weaponHandler = hit.transform.GetComponent<EnemyWeaponHandler>();
                if (weaponHandler != null)
                {
                    weaponHandler.DropBat();
                }

                // Play animation on enemy and freeze movement
                Animator enemyAnimator = hit.transform.GetComponentInChildren<Animator>();
                Debug.Log("Enemy Animator: " + enemyAnimator);
                if (enemyAnimator != null)
                {
                    StartCoroutine(PlayBananaHitAnimation(enemy.gameObject, enemyAnimator));
                }

                // Deal damage
                enemy.TakeDamage(damage);
            }
        }
    }

    System.Collections.IEnumerator PlayBananaHitAnimation(GameObject enemy, Animator animator)
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
