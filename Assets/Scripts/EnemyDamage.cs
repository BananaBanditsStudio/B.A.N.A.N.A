using UnityEngine;
using UnityEngine.UI;

public class EnemyDamage : MonoBehaviour
{
    public float maxHealth = 50f;
    public float health = 50f;
    private bool isDead = false;
    private bool isSlipping = false;
    private Animator animator;
    private Coroutine slippingRecoveryCoroutine;

    public Transform healthBarUI;
    public Image healthBarSprite;
    public GameObject damagePopupPrefab; // Reference to the damage popup prefab

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

    public void TakeDamage(float amount, bool isCriticalHit = false)
    {
        if (isDead) return; // Prevent taking damage after death

        health -= amount;
        UpdateHealthBar();
        
        // Spawn damage popup before checking death (so it shows even on killing blow)
        if (!isDead && damagePopupPrefab != null)
        {
            CreateDamagePopup(amount, isCriticalHit);
        }
        
        if (health <= 0f)
        {
            Die();
        }
    }
    
    private void CreateDamagePopup(float damageAmount, bool isCriticalHit)
    {
        if (damagePopupPrefab == null) return;
        
        // Spawn popup at chest/mid-torso level to stay visible even when camera is close
        // This prevents the popup from going off-screen when the top of the enemy is cut off
        Vector3 popupPosition = transform.position + Vector3.up * 0.4f;
        GameObject popupInstance = Instantiate(damagePopupPrefab, popupPosition, Quaternion.identity);
        
        // Setup the popup with damage value
        DamagePopup popup = popupInstance.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Setup(damageAmount, isCriticalHit);
        }
    }
    
    // Method to set slipping state (called by BananaPeelThrower)
    public void SetSlippingState(bool slipping)
    {
        isSlipping = slipping;
        
        if (slipping)
        {
            // Start recovery timer in case coroutine gets interrupted
            if (slippingRecoveryCoroutine != null)
                StopCoroutine(slippingRecoveryCoroutine);
            slippingRecoveryCoroutine = StartCoroutine(SlippingRecoveryTimer());
        }
        else
        {
            // Stop recovery timer if slipping is manually ended
            if (slippingRecoveryCoroutine != null)
            {
                StopCoroutine(slippingRecoveryCoroutine);
                slippingRecoveryCoroutine = null;
            }
        }
    }
    
    // Method to check if enemy is currently slipping
    public bool IsSlipping()
    {
        return isSlipping;
    }
    
    // Recovery timer in case slipping animation gets interrupted
    System.Collections.IEnumerator SlippingRecoveryTimer()
    {
        // Wait for the maximum slipping duration plus some buffer
        yield return new WaitForSeconds(6f); // 4.5s animation + 0.5s transition + 1s buffer
        
        // If still slipping after timeout, force recovery
        if (isSlipping && !isDead)
        {
            Debug.Log("Enemy slipping animation interrupted - forcing recovery");
            ForceSlippingRecovery();
        }
    }
    
    // Force recovery from slipping state
    public void ForceSlippingRecovery()
    {
        if (!isSlipping || isDead) return;
        
        Debug.Log("Forcing enemy slipping recovery");
        
        // Reset slipping state
        isSlipping = false;
        
        // Check if this enemy uses EnemyWithSM (state machine system)
        EnemyWithSM enemyWithSM = GetComponent<EnemyWithSM>();
        if (enemyWithSM != null)
        {
            UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && !navAgent.enabled)
            {
                navAgent.enabled = true;
            }
        }
        else
        {
            // For SimplePatrol enemies, re-enable movement components
            SimplePatrol patrolScript = GetComponent<SimplePatrol>();
            if (patrolScript != null && !patrolScript.enabled)
            {
                patrolScript.enabled = true;
            }
            
            UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && !navAgent.enabled)
            {
                navAgent.enabled = true;
            }
            
            CharacterController characterController = GetComponent<CharacterController>();
            if (characterController != null && !characterController.enabled)
            {
                characterController.enabled = true;
            }
        }
        
        // Reset animation to default state
        if (animator != null)
        {
            animator.CrossFade("Walk", 0.5f);
            animator.SetBool("IsLookingAround", false);
            animator.SetBool("IsAttacking", false);
        }
        
        // Stop recovery timer
        if (slippingRecoveryCoroutine != null)
        {
            StopCoroutine(slippingRecoveryCoroutine);
            slippingRecoveryCoroutine = null;
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
        
        // Track kill
        GameStatsUI.AddKill();
        
        // Stop any slipping animation if enemy is slipping
        if (isSlipping)
        {
            StopAllCoroutines(); // Stop any running slipping coroutines
            isSlipping = false;
        }
        
        // Stop recovery timer
        if (slippingRecoveryCoroutine != null)
        {
            StopCoroutine(slippingRecoveryCoroutine);
            slippingRecoveryCoroutine = null;
        }

        // Disable enemy movement script (only if not already disabled)
        SimplePatrol patrolScript = GetComponent<SimplePatrol>();
        if (patrolScript != null && patrolScript.enabled)
        {
            patrolScript.enabled = false;
        }


        EnemyWithSM enemyWithSM = GetComponent<EnemyWithSM>();
        if (enemyWithSM != null) {
            enemyWithSM.enabled = false;
        }

        StateMachine stateMachine = GetComponent<StateMachine>();
        if (stateMachine != null) {
            stateMachine.enabled = false;
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

