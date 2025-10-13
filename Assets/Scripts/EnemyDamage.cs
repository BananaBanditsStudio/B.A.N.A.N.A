using UnityEngine;
using UnityEngine.UI;

public class EnemyDamage : MonoBehaviour
{
    public float maxHealth = 50f;
    public float health = 50f;
    public float deathAnimationDuration = 2f; // Duration of death animation
    private bool isDead = false;

    public Transform healthBarUI;
    public Image healthBarSprite;



    private Camera cameraMain;

    void Start()
    {
        health = maxHealth;
        cameraMain = Camera.main;
        UpdateHealthBar();
    }

    void Update()
    {
        if (healthBarUI != null)
        {
            healthBarUI.rotation = Quaternion.LookRotation(healthBarUI.position - cameraMain.transform.position);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return; // Prevent taking damage after death

        health -= amount;
        UpdateHealthBar();
        if (health <= 0f)
        {
            Die();
        }
    }

    public void UpdateHealthBar()
    {
        float healthFraction = health / maxHealth;
        healthBarSprite.fillAmount = healthFraction;
    }

    void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        isDead = true;

        // Disable enemy movement script
        SimplePatrol patrolScript = GetComponent<SimplePatrol>();
        if (patrolScript != null)
        {
            patrolScript.enabled = false;
        }

        // Play death animation
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("isDead"); // Use trigger to transition to Death state
        }

        // Destroy after animation completes
        StartCoroutine(DestroyAfterAnimation());
    }

    System.Collections.IEnumerator DestroyAfterAnimation()
    {
        // Wait for death animation to complete
        yield return new WaitForSeconds(deathAnimationDuration);

        // Destroy the game object
        Destroy(gameObject);
    }
}

