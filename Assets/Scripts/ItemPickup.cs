using UnityEngine;
using UnityEngine.UI; // Or TMPro if using TextMeshPro

public class ItemPickup : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;         // Assign your main/player camera in Inspector
    public LayerMask interactLayerMask; // Set to 'Item' layer only

    [Header("Pickup Settings")]
    public float pickupTime = 2f;       // How long to hold button to pick up

    [Header("UI References")]
    public GameObject uiRoot;           // Root UI GameObject to show/hide
    public Image progressImage;         // Circular Image (fill set to Filled, Radial)
    public Text itemNameText;           // UI text for item name

    private Item currentItem;
    private float pickupElapsed = 0f;

    void Update()
    {
        // Always look for a new target each frame
        SelectItemFromRay();

        // If we have a valid item under crosshair
        if (currentItem)
        {
            if (uiRoot) uiRoot.SetActive(true);
            if (itemNameText) itemNameText.text = "Pickup " + currentItem.gameObject.name;

            // Hold to pick up
            if (Input.GetButton("Fire1"))
            {
                pickupElapsed += Time.deltaTime;
                if (progressImage) progressImage.fillAmount = Mathf.Clamp01(pickupElapsed / pickupTime);
                if (pickupElapsed >= pickupTime)
                {
                    PickupItem();
                }
            }
            else
            {
                pickupElapsed = 0f;
                if (progressImage) progressImage.fillAmount = 0;
            }
        }
        else
        {
            if (uiRoot) uiRoot.SetActive(false);
            pickupElapsed = 0f;
            if (progressImage) progressImage.fillAmount = 0;
        }
    }

    void SelectItemFromRay()
    {
        currentItem = null;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center of screen
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2f, interactLayerMask))
        {
            currentItem = hit.collider.GetComponent<Item>();
        }
    }

    void PickupItem()
    {
        if (currentItem)
        {
            // Do your inventory add logic here!
            Destroy(currentItem.gameObject);
            currentItem = null;
            pickupElapsed = 0f;
            if (progressImage) progressImage.fillAmount = 0;
        }
    }
}

// Example dummy Item script (empty placeholder)
public class Item : MonoBehaviour { }
