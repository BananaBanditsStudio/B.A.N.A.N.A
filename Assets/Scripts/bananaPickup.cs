using UnityEngine;
using System.Collections;


public class BananaPickup : MonoBehaviour
{

    public AudioClip pickupSound;
    public GameObject pickupVFX; // Optional particle effect
    public int bananaValue = 1;

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public int enemyCount = 5;
    public Transform spawnCenter;
    public float spawnRadius = 3f;


    public ObjectiveMarker bananaMarker;
    public ObjectiveMarker carMarker;
    public GameObject carTrigger;


    private bool canBeCollected = false;
    private bool isCollected = false;

    void Start()
    {
        // Wait 1 second before banana can be collected
        StartCoroutine(EnableCollectionAfterDelay(1f));

        // Set initial marker visibility
        if (bananaMarker != null)
            bananaMarker.gameObject.SetActive(true);

        if (carMarker != null)
            carMarker.gameObject.SetActive(false);
    }

    IEnumerator EnableCollectionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canBeCollected = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Prevent multiple triggers or early collection
        if (!canBeCollected || isCollected)
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player touched banana!");

            isCollected = true;

            // Toggle markers
            if (bananaMarker != null)
            {
                bananaMarker.gameObject.SetActive(false);
                Debug.Log("Banana marker hidden");
            }

            if (carMarker != null)
            {
                carMarker.gameObject.SetActive(true);
                Debug.Log("Car marker shown");
            }

            // Handle inventory
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddBananas(bananaValue);
                PlayerInventory.hasBanana = true;
                Debug.Log("Bananas stolen: " + bananaValue);
            }
            else
            {
                Debug.LogWarning("No PlayerInventory found on Player!");
            }

            // Play sound and VFX
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            if (pickupVFX != null)
                Instantiate(pickupVFX, transform.position, Quaternion.identity);

            // Spawn enemies
            SpawnEnemies();

            Debug.Log("Destroying banana...");
            Destroy(gameObject, 0.2f);
        }
    }



    void SpawnEnemies()
    {
        if (enemyPrefab == null || spawnCenter == null) return;

        for (int i = 0; i < enemyCount; i++)
        {
            // Random position around spawn center
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                1.2f,
                Random.Range(-spawnRadius, spawnRadius)
            );

            Vector3 spawnPosition = spawnCenter.position + randomOffset;

            // Instantiate enemy
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            Debug.Log($"Spawned enemy {i + 1} at {spawnPosition}");
        }
    }

}
