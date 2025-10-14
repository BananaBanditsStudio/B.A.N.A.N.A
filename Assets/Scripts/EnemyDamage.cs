using UnityEngine;
using UnityEngine.UI;

public class EnemyDamage : MonoBehaviour
{
    public float maxHealth = 50f;
    public float health = 50f;
    private bool isDead = false;
    private bool isSlipping = false;
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
    
    // Method to set slipping state (called by BananaPeelThrower)
    public void SetSlippingState(bool slipping)
    {
        isSlipping = slipping;
    }
    
    // Method to check if enemy is currently slipping
    public bool IsSlipping()
    {
        return isSlipping;
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
        
        // Stop any slipping animation if enemy is slipping
        if (isSlipping)
        {
            StopAllCoroutines(); // Stop any running slipping coroutines
            isSlipping = false;
        }

        // Disable enemy movement script (only if not already disabled)
        SimplePatrol patrolScript = GetComponent<SimplePatrol>();
        if (patrolScript != null && patrolScript.enabled)
        {
            patrolScript.enabled = false;
        }
        
        // Disable NavMeshAgent if present
        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.enabled = false;
        }
        
        // Disable CharacterController if present
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
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

