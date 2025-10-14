using UnityEngine;

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

            // Spawn enemies
            SpawnEnemies();

            Debug.Log("🍌 Destroying banana...");
            Destroy(gameObject, 0.1f);
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
