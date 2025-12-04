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
    public ObjectiveTracker objectiveTracker;  // Reference to ObjectiveTracker (usually on Benny)

    [Header("UI Prompt")]
    public GameObject interactPrompt; // "Press E to steal" UI
    public Camera playerCamera; // Assign your main camera in Inspector (auto-finds if null)

    private bool canBeCollected = false;
    private bool isCollected = false;
    private PlayerInventory cachedInventory;
    private GameObject playerObject;

    void Start()
    {
        // Auto-find player camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
        }

        // Auto-find player object
        if (playerCamera != null)
        {
            playerObject = playerCamera.transform.root.gameObject;
        }
        else
        {
            // Try to find by tag
            GameObject playerTagged = GameObject.FindGameObjectWithTag("Player");
            if (playerTagged != null)
            {
                playerObject = playerTagged;
                playerCamera = playerObject.GetComponentInChildren<Camera>();
            }
        }

        // Auto-find ObjectiveTracker if not assigned
        if (objectiveTracker == null && playerObject != null)
        {
            objectiveTracker = playerObject.GetComponent<ObjectiveTracker>();
            if (objectiveTracker == null)
            {
                objectiveTracker = playerObject.GetComponentInChildren<ObjectiveTracker>();
            }
        }

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

    void Update()
    {
        if (!canBeCollected || isCollected)
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);
            return;
        }

        bool lookingAtMe = false;
        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2.5f)) // Adjust distance as you like
            {
                if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                {
                    lookingAtMe = true;
                    // Cache inventory for efficiency
                    if (cachedInventory == null && playerObject != null)
                    {
                        cachedInventory = playerObject.GetComponent<PlayerInventory>() ?? playerObject.GetComponentInChildren<PlayerInventory>();
                    }
                }
            }
        }
        if (interactPrompt != null) interactPrompt.SetActive(lookingAtMe);
        if (lookingAtMe && Input.GetKeyDown(KeyCode.E))
        {
            TryCollect();
        }
    }

    public void TryCollect()
    {
        // Marker logic
        if (bananaMarker != null) bananaMarker.gameObject.SetActive(false);
        if (carMarker != null) carMarker.gameObject.SetActive(true);
        // Inventory
        if (cachedInventory != null)
        {
            cachedInventory.AddBananas(bananaValue);
            PlayerInventory.hasBanana = true;
        }
        else
        {
            Debug.LogWarning("No PlayerInventory found on Player!");
        }
        // Notify ObjectiveTracker if assigned (objective 3: baby banana)
        if (objectiveTracker != null)
        {
            objectiveTracker.CompleteBananaObjective();
        }
        // Audio/Visual
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (pickupVFX != null) Instantiate(pickupVFX, transform.position, Quaternion.identity);
        SpawnEnemies();
        isCollected = true;
        if (interactPrompt != null) interactPrompt.SetActive(false);
        Destroy(gameObject, 0.2f);
    }

    public void SpawnEnemies()
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
