using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Use rotation-based shake instead of position (prevents revealing player model)")]
    public bool useRotationShake = true;
    [Tooltip("If using position shake, use local position instead of world (safer for attached cameras)")]
    public bool useLocalPosition = true;
    
    // Position shake (if not using rotation)
    private Vector3 originalLocalPosition;
    private Vector3 originalWorldPosition;
    
    // Rotation shake
    private Quaternion originalRotation;
    
    private float shakeIntensity;
    private float shakeTimer;
    private bool isShaking;
    private bool hasParent;
    
    private void Start()
    {
        hasParent = transform.parent != null;
    }
    
    private void LateUpdate()
    {
        if (isShaking)
        {
            shakeTimer -= Time.deltaTime;
            
            if (shakeTimer > 0)
            {
                if (useRotationShake)
                {
                    // Rotation-based shake - won't reveal player model
                    // Use smaller rotation values (in degrees) for more subtle shake
                    float xRot = Random.Range(-1f, 1f) * shakeIntensity * 0.5f; // Pitch
                    float yRot = Random.Range(-1f, 1f) * shakeIntensity * 0.5f; // Yaw
                    float zRot = Random.Range(-1f, 1f) * shakeIntensity * 0.3f; // Roll (smaller for less disorientation)
                    
                    transform.localRotation = originalRotation * Quaternion.Euler(xRot, yRot, zRot);
                }
                else
                {
                    // Position-based shake (safer local position version)
                    Vector3 shakeOffset = new Vector3(
                        Random.Range(-1f, 1f) * shakeIntensity,
                        Random.Range(-1f, 1f) * shakeIntensity,
                        Random.Range(-1f, 1f) * shakeIntensity * 0.3f // Reduce Z shake to prevent revealing player
                    );
                    
                    if (useLocalPosition && hasParent)
                    {
                        transform.localPosition = originalLocalPosition + shakeOffset;
                    }
                    else
                    {
                        transform.position = originalWorldPosition + shakeOffset;
                    }
                }
            }
            else
            {
                // Reset to original state
                if (useRotationShake)
                {
                    transform.localRotation = originalRotation;
                }
                else
                {
                    if (useLocalPosition && hasParent)
                    {
                        transform.localPosition = originalLocalPosition;
                    }
                    else
                    {
                        transform.position = originalWorldPosition;
                    }
                }
                isShaking = false;
            }
        }
    }
    
    public void ShakeCamera(float intensity, float duration)
    {
        if (!isShaking)
        {
            // Store original state
            if (useRotationShake)
            {
                originalRotation = transform.localRotation;
            }
            else
            {
                if (useLocalPosition && hasParent)
                {
                    originalLocalPosition = transform.localPosition;
                }
                else
                {
                    originalWorldPosition = transform.position;
                }
            }
        }
        shakeIntensity = intensity;
        shakeTimer = duration;
        isShaking = true;
    }
}

