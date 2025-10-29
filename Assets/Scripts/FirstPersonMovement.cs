using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    private float originalHeight;
    private bool isCrouching = false;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalRotation = 0f;
    private Camera playerCamera;

    private Vector3 velocity;
    public bool isGrounded;
    private bool isSprinting = false;

    // Grapple integration
    [Header("Grapple Integration")]
    public bool freeze = false; // toggled by Grappling during pre-launch
    public bool activeGrapple = false; // while mid-arc
    private Vector3 externalGrappleVelocity = Vector3.zero; // horizontal component driven externally
    private bool enableMovementOnNextGround = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = Camera.main;

        originalHeight = controller.height;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // --- Ground Check ---
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // keep player grounded
        }

        // --- Movement --- (skip if frozen or grappling)
        if (!freeze)
        {
            float currentSpeed = walkSpeed;
            if (isCrouching) currentSpeed = crouchSpeed;
            else if (isSprinting) currentSpeed = sprintSpeed;

            if (!activeGrapple)
            {
                Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
                controller.Move(move * currentSpeed * Time.deltaTime);
            }
        }

        // --- Look ---
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime * 100f;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime * 100f;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Apply externally-driven horizontal arc while grappling
        if (activeGrapple)
        {
            controller.Move(externalGrappleVelocity * Time.deltaTime);
        }

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // If we touched ground after a grapple, clear restrictions
        if (activeGrapple && isGrounded && enableMovementOnNextGround)
        {
            ResetRestrictions();
        }
    }

    // --- Input System Callbacks ---
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // === Grapple support methods for Grappling.cs ===
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;
        Vector3 initial = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        externalGrappleVelocity = new Vector3(initial.x, 0f, initial.z);
        Debug.Log("initial: " + initial);
        Debug.Log("External Grapple Velocity: " + externalGrappleVelocity);
        velocity.y = initial.y;
        enableMovementOnNextGround = true;
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
        freeze = false;
        externalGrappleVelocity = Vector3.zero;
        enableMovementOnNextGround = false;
    }

    private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float g = gravity; // negative value
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        // Vertical component (always valid since g is negative and height > 0)
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2f * g * Mathf.Max(0.01f, trajectoryHeight));

        // Time to apex and from apex to target. The second term can be invalid if the target is
        // much higher than the chosen apex. Clamp to 0 in that case so we still get forward speed.
        float timeUp = Mathf.Sqrt(Mathf.Max(0.0001f, -2f * trajectoryHeight / g));
        float denomDown = 2f * (displacementY - trajectoryHeight) / g; // g is negative
        float timeDown = denomDown > 0f ? Mathf.Sqrt(denomDown) : 0f;
        float totalTime = Mathf.Max(0.0001f, timeUp + timeDown);

        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleCrouch();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
            isSprinting = true;
        else if (context.canceled)
            isSprinting = false;
    }

    private void ToggleCrouch()
    {
        if (isCrouching)
        {
            controller.height = originalHeight;
            isCrouching = false;
        }
        else
        {
            controller.height = crouchHeight;
            isCrouching = true;
        }
    }
}
