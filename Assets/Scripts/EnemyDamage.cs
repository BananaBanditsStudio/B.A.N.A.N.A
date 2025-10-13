using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public float health = 50f;
    public float deathAnimationDuration = 2f; // Duration of death animation
    private bool isDead = false;

    public void TakeDamage(float amount)
    {
        if (isDead) return; // Prevent taking damage after death

        health -= amount;
        if (health <= 0f)
        {
            Die();
        }
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

