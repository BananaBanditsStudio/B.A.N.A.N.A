using UnityEngine;

public class ParallaxLayer3D : MonoBehaviour
{
    [Tooltip("Foreground ~0.6-0.8, background ~0.05-0.2")]
    public float parallaxSpeed = 0.2f;

    public void Scroll(float amount)
    {
        // Move opposite the car's motion to simulate world sliding by.
        transform.position -= new Vector3(amount * parallaxSpeed, 0f, 0f);
    }
}
