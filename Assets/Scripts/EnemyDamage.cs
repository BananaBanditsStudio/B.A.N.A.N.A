using UnityEngine;
using UnityEngine.UI;

public class EnemyDamage : MonoBehaviour
{
    public float maxHealth = 50f;
    public float health = 50f;
    private bool isDead = false;
    private Animator animator;

    public Transform healthBarUI;
    public Image healthBarSprite;



    private Camera cameraMain;

    void Start()
    {
        health = maxHealth;
        cameraMain = Camera.main;
        animator = GetComponentInChildren<Animator>();
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

        // Immediately destroy the vision fan (FieldOfView3D component)
        FieldOfView3D fieldOfView = GetComponentInChildren<FieldOfView3D>();
        if (fieldOfView != null)
        {
            Destroy(fieldOfView.gameObject);
        }

        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger("isDead"); // Use trigger to transition to Death state
        }

        // Destroy after animation completes
        StartCoroutine(DestroyAfterAnimation());
    }

    System.Collections.IEnumerator DestroyAfterAnimation()
    {
        if (animator != null)
        {
            // Wait for the death animation to start
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Death"));
            
            // Wait for the death animation to complete
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);
        }
        else
        {
            // Fallback: if no animator, wait a short time then destroy
            yield return new WaitForSeconds(0.5f);
        }

        // Destroy the game object immediately after animation completes
        Destroy(gameObject);
    }
}

