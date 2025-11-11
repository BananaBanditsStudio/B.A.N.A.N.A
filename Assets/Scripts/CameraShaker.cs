using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeIntensity;
    private float shakeTimer;
    private bool isShaking;
    
    private void LateUpdate()
    {
        if (isShaking)
        {
            shakeTimer -= Time.deltaTime;
            
            if (shakeTimer > 0)
            {
                // Remove previous offset first
                transform.position -= shakeOffset;
                
                // Calculate new shake offset
                shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * shakeIntensity,
                    Random.Range(-1f, 1f) * shakeIntensity,
                    Random.Range(-1f, 1f) * shakeIntensity
                );
                
                // Apply new shake offset to current position
                transform.position += shakeOffset;
            }
            else
            {
                // Remove shake offset when done
                transform.position -= shakeOffset;
                shakeOffset = Vector3.zero;
                isShaking = false;
            }
        }
    }
    
    public void ShakeCamera(float intensity, float duration)
    {
        // If already shaking, remove previous offset first
        if (isShaking)
        {
            transform.position -= shakeOffset;
            shakeOffset = Vector3.zero;
        }
        
        shakeIntensity = intensity;
        shakeTimer = duration;
        isShaking = true;
    }
}

