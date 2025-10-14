using UnityEngine;

public class BananaPeelThrow : MonoBehaviour
{
    [Header("Throw Settings")]
    public float throwSpeed = 12f;
    public float arcHeight = 2f;
    public float rotationSpeed = 180f;
    public float lifetime = 10f;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float throwForce;
    private float throwTime;
    private float totalThrowTime;
    private bool isThrowing = false;
    
    void Start()
    {
        // Destroy after lifetime to prevent accumulation
        Destroy(gameObject, lifetime);
    }
    
    public void InitializeThrow(Vector3 start, Vector3 target, float force)
    {
        startPosition = start;
        targetPosition = target;
        throwForce = force;
        
        // Calculate throw trajectory
        float distance = Vector3.Distance(startPosition, targetPosition);
        totalThrowTime = distance / (throwSpeed * (force / 10f));
        
        // Start the throw
        isThrowing = true;
        throwTime = 0f;
    }
    
    void Update()
    {
        if (!isThrowing) return;
        
        throwTime += Time.deltaTime;
        float progress = throwTime / totalThrowTime;
        
        if (progress >= 1f)
        {
            // Land at target position
            transform.position = targetPosition;
            isThrowing = false;
            return;
        }
        
        // Calculate position along the arc
        Vector3 currentPosition = CalculateArcPosition(progress);
        transform.position = currentPosition;
        
        // Add rotation for realistic spinning effect
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
    
    Vector3 CalculateArcPosition(float progress)
    {
        // Linear interpolation between start and target
        Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        
        // Add arc height using a parabola
        float arcOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
        linearPosition.y += arcOffset;
        
        return linearPosition;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Stop throwing when hitting ground or obstacles
        if (other.CompareTag("Ground") || other.CompareTag("Obstacle"))
        {
            isThrowing = false;
        }
    }
}
