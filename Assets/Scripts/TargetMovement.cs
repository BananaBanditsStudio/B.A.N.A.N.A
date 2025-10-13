using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveDistance = 5f; // How far to move left/right
    public float moveSpeed = 2f; // How fast to move

    private Vector3 startPosition;
    private float direction = 1f; // 1 for right, -1 for left

    void Start()
    {
        // Store the starting position
        startPosition = transform.position;
    }

    void Update()
    {
        // Move the target along X axis
        transform.position += Vector3.right * direction * moveSpeed * Time.deltaTime;

        // Calculate distance from start
        float distanceMoved = transform.position.x - startPosition.x;

        // Reverse direction when reaching the limit
        if (Mathf.Abs(distanceMoved) >= moveDistance)
        {
            direction *= -1f; // Flip direction

            // Clamp position to prevent overshooting
            Vector3 clampedPos = transform.position;
            clampedPos.x = startPosition.x + (moveDistance * Mathf.Sign(distanceMoved));
            transform.position = clampedPos;
        }
    }
}
