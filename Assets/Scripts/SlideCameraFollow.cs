using UnityEngine;

public class SideCameraFollow : MonoBehaviour
{
    public Transform target;       // the car
    public Vector3 offset = new Vector3(0, 2, -10);
    public float smoothSpeed = 3f;
    public bool lockY = true;      // keep camera height fixed for side view

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        if (lockY)
            desired.y = transform.position.y;

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
