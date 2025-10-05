using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField]
    private float distance = 3f;
    [SerializeField]
    private LayerMask mask;
    private PlayerUI playerUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get camera from the child of the player
        cam = Camera.main;
        playerUI = GetComponent<PlayerUI>();
    }

    // Update is called once per frame
    void Update()
    {
        playerUI.UpdateText(string.Empty);
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red, 0.1f);

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable != null){
                playerUI.UpdateText(interactable.promptMessage);
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactable.BaseInteract();
                }
            }
        }
    }
}
