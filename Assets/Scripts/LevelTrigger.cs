using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    public string nextSceneName;
    private GameStateManager gameStateManager;
    [SerializeField] private FadeTransition fadeTransition;

    [Header("UI Prompt")]
    public GameObject interactPrompt; // Drag your "Press E to drive" UI here
    public Camera playerCamera; // Drag your player camera here (auto-finds if null)
    public float interactDistance = 3f; // Adjust for how far away 'drive' can be triggered
    public ObjectiveTracker objectiveTracker;  // Reference to ObjectiveTracker (usually on Benny, auto-finds if null)

    private bool isCollected => PlayerInventory.hasBanana; // For clarity
    private bool isLevelEnding = false;
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

        // Get or create GameStateManager
        gameStateManager = GameStateManager.Instance;
        if (gameStateManager == null)
        {
            GameObject gameStateGO = new GameObject("GameStateManager");
            gameStateManager = gameStateGO.AddComponent<GameStateManager>();
        }
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (isLevelEnding) return;
        bool showPrompt = false;
        if (playerCamera && isCollected)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                if (hit.collider && hit.collider.gameObject == this.gameObject)
                {
                    showPrompt = true;
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        TryDrive();
                    }
                }
            }
        }
        if (interactPrompt) interactPrompt.SetActive(showPrompt);
    }

    void TryDrive()
    {
        isLevelEnding = true;
        Debug.Log("Player starting drive transition...");
        
        // Notify ObjectiveTracker if assigned (objective 4: getaway)
        if (objectiveTracker != null)
        {
            objectiveTracker.CompleteGetawayObjective();
        }
        
        if (gameStateManager) gameStateManager.ResetState();
        Time.timeScale = 1f;
        if (nextSceneName.ToLower().Contains("title"))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Preparing for TitleScreen - cursor unlocked");
        }
        if (fadeTransition)
        {
            fadeTransition.nextSceneName = nextSceneName;
            fadeTransition.StartFadeOut();
        }
        else
        {
            Debug.LogWarning("FadeTransition not assigned! Loading instantly.");
            SceneManager.LoadScene(nextSceneName);
        }
        if (interactPrompt) interactPrompt.SetActive(false);
    }
}
