using UnityEngine;

public class BananaPickup : MonoBehaviour
{

    public AudioClip pickupSound;
    public GameObject pickupVFX; // Optional particle effect
    public int bananaValue = 1;

    private bool isCollected = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player touched banana");

        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("✅ Player tag confirmed!");

            isCollected = true;

            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                Debug.Log("Bananas stolen: " + bananaValue);
                inventory.AddBananas(bananaValue);
            }
            else
            {
                Debug.Log("🚫 No PlayerInventory found on Player!");
            }

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            if (pickupVFX != null)
                Instantiate(pickupVFX, transform.position, Quaternion.identity);

            Debug.Log("🍌 Destroying banana...");
            Destroy(gameObject, 0.1f);
        }
    }

}
