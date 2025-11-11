using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    private Vector3 originalPosition;
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
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * shakeIntensity,
                    Random.Range(-1f, 1f) * shakeIntensity,
                    Random.Range(-1f, 1f) * shakeIntensity
                );
                transform.position = originalPosition + shakeOffset;
            }
            else
            {
                transform.position = originalPosition;
                isShaking = false;
            }
        }
    }
    
    public void ShakeCamera(float intensity, float duration)
    {
        if (!isShaking)
        {
            originalPosition = transform.position;
        }
        shakeIntensity = intensity;
        shakeTimer = duration;
        isShaking = true;
    }
}

