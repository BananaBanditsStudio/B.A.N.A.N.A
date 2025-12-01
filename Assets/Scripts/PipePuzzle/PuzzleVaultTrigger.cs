using UnityEngine;

/// <summary>
/// Monitors a specific PipePuzzle instance and triggers a vault animator when that puzzle is solved.
/// Attach this to the vault GameObject or any object in the scene.
/// </summary>
public class PuzzleVaultTrigger : MonoBehaviour
{
    [Header("Puzzle Reference")]
    [Tooltip("The specific PipePuzzle that controls this vault")]
    public PipePuzzle targetPuzzle;
    
    [Header("Vault Settings")]
    [Tooltip("The Animator component on the vault")]
    public Animator vaultAnimator;
    [Tooltip("The trigger parameter name in the Animator")]
    public string openTriggerName = "Open";
    
    [Header("Optional Audio")]
    public AudioSource audioSource;
    public AudioClip vaultOpenSound;
    
    private bool hasTriggered = false;
    
    void Update()
    {
        // Don't check if already triggered
        if (hasTriggered) return;
        
        // Check if the specific puzzle instance is solved
        if (targetPuzzle != null && targetPuzzle.IsSolvedInstance)
        {
            TriggerVaultOpen();
        }
    }
    
    void TriggerVaultOpen()
    {
        hasTriggered = true;
        
        // Trigger the animator
        if (vaultAnimator != null)
        {
            vaultAnimator.SetTrigger(openTriggerName);
            Debug.Log($"[PuzzleVaultTrigger] Vault opened! Triggered '{openTriggerName}' on {vaultAnimator.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[PuzzleVaultTrigger] No Animator assigned!");
        }
        
        // Play sound if assigned
        if (audioSource != null && vaultOpenSound != null)
        {
            audioSource.PlayOneShot(vaultOpenSound);
        }
    }
    
    /// <summary>
    /// Call this to manually reset the trigger (e.g., if puzzle can be re-solved)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}

