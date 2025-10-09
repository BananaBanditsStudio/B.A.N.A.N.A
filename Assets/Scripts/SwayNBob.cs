using UnityEngine;

public class SwayNBobScript : MonoBehaviour
{
    public FirstPersonController mover;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    Vector3 swayPos;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    Vector3 swayEulerRot;

    public float smooth = 10f;
    float smoothRot = 12f;

    [Header("Bobbing")]
    public float bobSpeed = 8f;
    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    Vector3 bobPosition;

    public float bobExaggeration = 1f;

    [Header("Bob Rotation")]
    public Vector3 multiplier = new Vector3(1f, 1f, 1f);
    Vector3 bobEulerRotation;

    Vector2 walkInput;
    Vector2 lookInput;
    float bobTimer;

    void Update()
    {
        if (mover == null) return;

        GetInput();
        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();
        CompositePositionRotation();
    }

    void GetInput()
    {
        walkInput.x = Input.GetAxisRaw("Horizontal");
        walkInput.y = Input.GetAxisRaw("Vertical");
        walkInput = walkInput.normalized;

        lookInput.x = Input.GetAxis("Mouse X");
        lookInput.y = Input.GetAxis("Mouse Y");
    }

    void Sway()
    {
        Vector3 invertLook = lookInput * -step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);
        swayPos = invertLook;
    }

    void SwayRotation()
    {
        Vector2 invertLook = lookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    void BobOffset()
    {
        if (mover.isGrounded && walkInput.magnitude > 0)
            bobTimer += Time.deltaTime * bobSpeed * bobExaggeration;
        else
            bobTimer += Time.deltaTime * bobSpeed;

        float curveSin = Mathf.Sin(bobTimer);
        float curveCos = Mathf.Cos(bobTimer);

        bobPosition.x = (curveCos * bobLimit.x * (mover.isGrounded ? 1 : 0)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (walkInput.y * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    void BobRotation()
    {
        float curveSin = Mathf.Sin(2 * bobTimer);
        float curveCos = Mathf.Cos(bobTimer);

        if (walkInput != Vector2.zero)
        {
            bobEulerRotation.x = multiplier.x * curveSin;
            bobEulerRotation.y = multiplier.y * curveCos;
            bobEulerRotation.z = multiplier.z * curveCos * walkInput.x;
        }
        else
        {
            bobEulerRotation = Vector3.Lerp(bobEulerRotation, Vector3.zero, Time.deltaTime * 5f);
        }
    }

    void CompositePositionRotation()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, swayPos + bobPosition, Time.deltaTime * smooth);
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation),
            Time.deltaTime * smoothRot
        );
    }
}
