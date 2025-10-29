using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPush : MonoBehaviour
{
    public float pushPower = 2.0f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // No rigidbody or object is kinematic → do nothing
        if (body == null || body.isKinematic)
            return;

        // Only push rigidbodies along the horizontal plane
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // Apply velocity-based push
        body.linearVelocity = pushDir * pushPower;
    }
}
