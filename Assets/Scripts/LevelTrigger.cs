using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    public string nextSceneName;
    private GameStateManager gameStateManager;
    [SerializeField] private FadeTransition fadeTransition;

    [Header("UI Prompt")]
    public GameObject interactPrompt; // Drag your "Press E to drive" UI here
    public Camera playerCamera; // Drag your player camera here
    public float interactDistance = 3f; // Adjust for how far away 'drive' can be triggered

    private bool isCollected => PlayerInventory.hasBanana; // For clarity
    private bool isLevelEnding = false;

    void Start()
    {
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
