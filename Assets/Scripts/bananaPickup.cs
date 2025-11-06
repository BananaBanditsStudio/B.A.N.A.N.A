using UnityEngine;

public class BananaPickup : MonoBehaviour
{
    public AudioClip pickupSound;
    public GameObject pickupVFX;
    public int bananaValue = 1;

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public int enemyCount = 5;
    public Transform spawnCenter;
    public float spawnRadius = 3f;

    public ObjectiveMarker bananaMarker;
    public ObjectiveMarker carMarker;
    public GameObject carTrigger;

    [Header("UI Prompt")]
    public GameObject interactPrompt; // "Press E to steal" UI

    private bool canBeCollected = false;
    private bool isCollected = false;
    private bool isPlayerInRange = false;
    private PlayerInventory cachedInventory; // optional cache

    void Start()
    {
        StartCoroutine(EnableCollectionAfterDelay(1f));

        if (bananaMarker != null) bananaMarker.gameObject.SetActive(true);
        if (carMarker != null) carMarker.gameObject.SetActive(false);

        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    System.Collections.IEnumerator EnableCollectionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canBeCollected = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canBeCollected || isCollected) return;
        if (!other.CompareTag("Player")) return;

        // Find inventory on the player (supports child colliders)
        cachedInventory = other.GetComponentInParent<PlayerInventory>();

        isPlayerInRange = true;

        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = false;
        cachedInventory = null;

        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (!canBeCollected || isCollected) return;

        // Legacy Input example; swap with new Input System if you use it
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            TryCollect();
        }
    }

    private void TryCollect()
    {
        // Safety: resolve inventory if not cached
        if (cachedInventory == null)
        {
            // Attempt to find any overlapping player collider again if needed
            // Optional, usually not necessary if cached in OnTriggerEnter
        }

        // Toggle markers immediately
        if (bananaMarker != null) bananaMarker.gameObject.SetActive(false);
        if (carMarker != null) carMarker.gameObject.SetActive(true);

        // Handle inventory
        if (cachedInventory != null)
        {
            cachedInventory.AddBananas(bananaValue);
            PlayerInventory.hasBanana = true;
        }
        else
        {
            Debug.LogWarning("No PlayerInventory found on Player!");
        }

        // A/V
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (pickupVFX != null) Instantiate(pickupVFX, transform.position, Quaternion.identity);

        // Spawn enemies
        SpawnEnemies();

        // Prevent re-use and hide prompt
        isCollected = true;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        Destroy(gameObject, 0.2f);
    }

    void SpawnEnemies()
    {
        if (enemyPrefab == null || spawnCenter == null) return;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                1.2f,
                Random.Range(-spawnRadius, spawnRadius)
            );

            Vector3 spawnPosition = spawnCenter.position + randomOffset;
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
