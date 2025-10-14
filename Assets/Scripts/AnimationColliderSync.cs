using UnityEngine;

public class AnimationColliderSync : MonoBehaviour
{
    [Header("Collider Sync Settings")]
    public bool syncWithAnimation = true;
    public float syncSpeed = 10f;
    
    private CharacterController characterController;
    private CapsuleCollider capsuleCollider;
    private Animator animator;
    private Vector3 lastPosition;
    private bool isSlipping = false;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }
    
    void Update()
    {
        if (!syncWithAnimation) return;
        
        // Check if enemy is slipping
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            isSlipping = stateInfo.IsName("Slip") || stateInfo.IsName("Slipping");
        }
        
        // If slipping and we have a collider, sync it with animation
        if (isSlipping && capsuleCollider != null)
        {
            SyncColliderWithAnimation();
        }
    }
    
    void SyncColliderWithAnimation()
    {
        // Get the current position from the animation
        Vector3 currentPosition = transform.position;
        
        // If the position has changed (due to animation), update the collider
        if (Vector3.Distance(currentPosition, lastPosition) > 0.01f)
        {
            // Move the collider to match the animated position
            if (capsuleCollider != null)
            {
                // The collider should automatically follow the transform
                // But we can ensure it's properly positioned
                capsuleCollider.center = Vector3.zero;
            }
            
            lastPosition = currentPosition;
        }
    }
    
    // Method to force sync (can be called externally)
    public void ForceSync()
    {
        if (capsuleCollider != null)
        {
            capsuleCollider.center = Vector3.zero;
        }
    }
    
    // Method to check if currently slipping
    public bool IsSlipping()
    {
        return isSlipping;
    }
}
