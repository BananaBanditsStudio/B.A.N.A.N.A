using UnityEngine;
using System;

public class BananaPeelThrow : MonoBehaviour
{
    [Header("Throw Settings")]
    public float throwSpeed = 12f;
    public float arcHeight = 2f;
    public float rotationSpeed = 180f;
    public float lifetime = 10f;
    public float fixedThrowTime = 0.4f; // Fixed travel time regardless of distance
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float throwForce;
    private float throwTime;
    private float totalThrowTime;
    private bool isThrowing = false;
    public Action onArrived; // callback when peel arrives at target
    
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
        
        // Use fixed travel time for fast, consistent throws regardless of distance
        totalThrowTime = fixedThrowTime;
        
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
            // signal arrival for animation/damage sync
            try { onArrived?.Invoke(); } catch {}
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
        
        // Scale arc height based on distance for more realistic arcs at different ranges
        float distance = Vector3.Distance(startPosition, targetPosition);
        float scaledArcHeight = arcHeight * Mathf.Clamp(distance / 10f, 0.5f, 2f);
        
        // Add arc height using a parabola
        float arcOffset = Mathf.Sin(progress * Mathf.PI) * scaledArcHeight;
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
