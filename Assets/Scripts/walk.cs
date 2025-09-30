using UnityEngine;
using UnityEngine.InputSystem;

public class walk : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    
    [Header("Animation")]
    public Animator animator;
    
    private CharacterController characterController;
    private Vector3 moveDirection;
    private bool isMoving;
    private Vector2 inputVector;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the CharacterController component
        characterController = GetComponent<CharacterController>();
        
        // If no animator is assigned, try to find one on this object
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleAnimation();
    }
    
    // Input System callback for movement input
    public void OnMove(InputAction.CallbackContext context)
    {
        inputVector = context.ReadValue<Vector2>();
    }
    
    void HandleMovement()
    {
        // Get input from Input System
        float horizontal = inputVector.x;
        float vertical = inputVector.y;
        
        // Alternative: Direct keyboard input using Input System
        if (horizontal == 0 && vertical == 0)
        {
            if (Keyboard.current != null)
            {
                horizontal = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
                vertical = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
            }
        }
        
        // Debug: Print input values to console
        if (horizontal != 0 || vertical != 0)
        {
            Debug.Log($"Input: H={horizontal}, V={vertical}");
        }
        
        // Calculate movement direction
        moveDirection = new Vector3(horizontal, 0, vertical);
        moveDirection = moveDirection.normalized;
        
        // Check if character is moving
        isMoving = moveDirection.magnitude > 0.1f;
        
        // Debug: Print movement status
        if (isMoving)
        {
            Debug.Log($"Moving in direction: {moveDirection}");
        }
        
        if (isMoving)
        {
            // Move the character using CharacterController if available
            if (characterController != null)
            {
                characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
                Debug.Log("Using CharacterController for movement");
            }
            else
            {
                // Fallback: Use Transform.Translate for basic movement
                transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
                Debug.Log("Using Transform.Translate for movement");
            }
            
            // Rotate character to face movement direction
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    void HandleAnimation()
    {
        if (animator != null)
        {
            // Set walking animation based on movement
            animator.SetBool("IsWalking", isMoving);
        }
    }
}
