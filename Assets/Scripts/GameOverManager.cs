using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;
    public TextMeshProUGUI gameOverTitle;
    public TextMeshProUGUI gameOverMessage;
    
    [Header("Settings")]
    public string currentSceneName; // Will be set automatically
    public string titleScreenSceneName = "TitleScreen";
    public float fadeInDuration = 1f;
    public float delayBeforeShow = 2f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gameOverSound;
    public AudioClip buttonClickSound;
    
    [Header("Visual Effects")]
    public Image fadeOverlay;
    public float fadeSpeed = 1f;
    
    [Header("Death Camera")]
    public Camera deathCamera;
    public float deathAnimationDuration = 3f;
    
    private bool isGameOver = false;
    private PlayerHealth playerHealth;
    private CanvasGroup canvasGroup;
    private GameStateManager gameStateManager;
    private Camera mainCamera;
    
    void Start()
    {
        // Get current scene name
        currentSceneName = SceneManager.GetActiveScene().name;
        
        // Get or create GameStateManager
        gameStateManager = GameStateManager.Instance;
        if (gameStateManager == null)
        {
            GameObject gameStateGO = new GameObject("GameStateManager");
            gameStateManager = gameStateGO.AddComponent<GameStateManager>();
        }
        
        // Find player health component
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("GameOverManager: PlayerHealth component not found!");
            return;
        }
        
        // Set up UI
        SetupUI();
        
        // Initially hide the game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Set up button listeners
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            restartButton.onClick.AddListener(RestartGame);
            Debug.Log("[GameOverManager] Restart button listener added");
        }
        else
        {
            Debug.LogError("[GameOverManager] restartButton is NULL! Assign it in the Inspector.");
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            quitButton.onClick.AddListener(QuitToTitleScreen);
            Debug.Log("[GameOverManager] Quit button listener added");
        }
        else
        {
            Debug.LogError("[GameOverManager] quitButton is NULL! Assign it in the Inspector.");
        }
        
        // Get or add canvas group for fade effects
        canvasGroup = gameOverPanel?.GetComponent<CanvasGroup>();
        if (canvasGroup == null && gameOverPanel != null)
        {
            canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }
        
        // Initialize CanvasGroup to hidden state (panel is SetActive(false) anyway, but be explicit)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        // Cache main camera
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        // Check if player is dead (only check once per frame, early exit if already game over)
        if (isGameOver || playerHealth == null) return;
        
        if (playerHealth.Health <= 0)
        {
            TriggerGameOver();
        }
    }
    
    void SetupUI()
    {
        // Set up title
        if (gameOverTitle != null)
        {
            gameOverTitle.text = "GAME OVER";
            gameOverTitle.color = Color.red;
        }
        
        // Set up message
        if (gameOverMessage != null)
        {
            gameOverMessage.text = "You have been defeated...";
            gameOverMessage.color = Color.white;
        }
        
        // Set up buttons
        if (restartButton != null)
        {
            SetupButton(restartButton, "Restart Level");
        }
        
        if (quitButton != null)
        {
            SetupButton(quitButton, "Quit to Title");
        }
    }
    
    void SetupButton(Button button, string text)
    {
        // Set button text
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = text;
            buttonText.color = Color.white;
        }
        
        // Set button colors
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        button.colors = colors;
    }
    
    public void TriggerGameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        // Play game over sound
        PlaySound(gameOverSound);
        
        // Start the game over sequence
        StartCoroutine(GameOverSequence());
    }
    
    System.Collections.IEnumerator GameOverSequence()
    {
        // Block input but DON'T freeze time yet (so animation can play)
        if (gameStateManager != null)
        {
            gameStateManager.BlockInput(true);
        }
        
        // Trigger death animation FIRST
        if (playerHealth != null)
        {
            playerHealth.TriggerDeath();
            
            // Disable all scripts on the player except Animator
            MonoBehaviour[] playerScripts = playerHealth.GetComponents<MonoBehaviour>();
            foreach (var script in playerScripts)
            {
                if (script != null && !(script is Animator))
                {
                    script.enabled = false;
                }
            }
            
            // Also disable scripts on children (like camera scripts)
            MonoBehaviour[] childScripts = playerHealth.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in childScripts)
            {
                if (script != null && !(script is Animator))
                {
                    script.enabled = false;
                }
            }
            
            // Hide weapons by finding and disabling WeaponHolder
            Transform[] allChildren = playerHealth.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name == "WeaponHolder" || child.name == "WeaponPivot")
                {
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }
        
        // Switch to death camera
        if (deathCamera != null && mainCamera != null)
        {
            // Only disable camera component, not the whole GameObject
            mainCamera.enabled = false;
            
            // Disable AudioListener on main camera if it has one
            AudioListener mainListener = mainCamera.GetComponent<AudioListener>();
            if (mainListener != null) mainListener.enabled = false;
            
            // Enable death camera GameObject (it was disabled in scene)
            deathCamera.gameObject.SetActive(true);
        }
        
        // Wait for death animation (use realtime for consistent timing)
        yield return new WaitForSecondsRealtime(deathAnimationDuration);
        
        // NOW freeze time and set game over
        if (gameStateManager != null)
        {
            gameStateManager.SetGameOver(true);
        }
        
        // Unlock cursor BEFORE showing UI so it's ready for interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Show game over panel and enable interaction
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // Get or refresh canvasGroup reference in case it changed
            canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }
            
            // IMMEDIATELY enable interaction - don't wait for fade
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            // Also ensure buttons are interactable
            if (restartButton != null) restartButton.interactable = true;
            if (quitButton != null) quitButton.interactable = true;
            
            // FIX COMMON UI ISSUES
            EnsureUICanReceiveInput();
            
            // IMPORTANT: Force-disable any pause menu that might be lingering
            DisablePauseMenuUI();
            
            // Also close any active puzzle that might be interfering
            CloseAnyActivePuzzle();
            
            Debug.Log($"[GameOverManager] Panel shown. CanvasGroup interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");
        }
        else
        {
            Debug.LogError("[GameOverManager] gameOverPanel is NULL! Assign it in the Inspector.");
        }
        
        // Fade in the game over screen (visual only, interaction already enabled)
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeInGameOver());
        }
        
        // Ensure cursor is still unlocked after fade (safety check)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    System.Collections.IEnumerator FadeInGameOver()
    {
        // Enable interaction immediately so buttons are clickable during fade
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    public void RestartGame()
    {
        PlaySound(buttonClickSound);
        
        // Reset game state before scene change
        if (gameStateManager != null)
        {
            gameStateManager.ResetState();
        }
        
        // Resume time scale before scene change
        Time.timeScale = 1f;
        
        // Reload the current scene
        SceneManager.LoadScene(currentSceneName);
    }
    
    public void QuitToTitleScreen()
    {
        PlaySound(buttonClickSound);
        
        // Reset game state before scene change
        if (gameStateManager != null)
        {
            gameStateManager.ResetState();
        }
        
        // Resume time scale before scene change
        Time.timeScale = 1f;
        
        // Load title screen scene
        SceneManager.LoadScene(titleScreenSceneName);
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Public getter for game over state
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    /// <summary>
    /// Close any active puzzle to prevent interference
    /// </summary>
    private void CloseAnyActivePuzzle()
    {
        // Find and close any active puzzle
        PipePuzzle[] puzzles = FindObjectsByType<PipePuzzle>(FindObjectsSortMode.None);
        foreach (var puzzle in puzzles)
        {
            if (puzzle != null && PipePuzzle.IsAnyPuzzleActive)
            {
                // The puzzle creates a canvas child - find and destroy it
                Transform puzzleCanvas = puzzle.transform.Find("PipePuzzleCanvas");
                if (puzzleCanvas != null)
                {
                    Destroy(puzzleCanvas.gameObject);
                }
                
                // Also search in the canvas if puzzle has a canvas reference
                Canvas canvas = puzzle.canvas;
                if (canvas != null)
                {
                    Transform canvasChild = canvas.transform.Find("PipePuzzleCanvas");
                    if (canvasChild != null)
                    {
                        Destroy(canvasChild.gameObject);
                        Debug.Log("[GameOverManager] Destroyed active puzzle canvas");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Force-disable any pause menu UI to prevent interference
    /// </summary>
    private void DisablePauseMenuUI()
    {
        // Find and disable any PauseMenuUI in the scene
        PauseMenuUI pauseUI = FindFirstObjectByType<PauseMenuUI>();
        if (pauseUI != null)
        {
            // Force hide and deactivate
            var cg = pauseUI.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            pauseUI.gameObject.SetActive(false);
            Debug.Log("[GameOverManager] Disabled PauseMenuUI to prevent interference");
        }
        
        // Also find the PauseMenu controller and reset its state
        PauseMenu pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (pauseMenu != null && pauseMenu.pauseMenuUI != null)
        {
            pauseMenu.pauseMenuUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// Diagnose and fix common UI interaction issues
    /// </summary>
    private void EnsureUICanReceiveInput()
    {
        // 1. Check EventSystem
        if (EventSystem.current == null)
        {
            Debug.LogError("[GameOverManager] NO EVENTSYSTEM FOUND! Creating one...");
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            // Try new input system first, fall back to standalone
#if ENABLE_INPUT_SYSTEM
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemGO.AddComponent<StandaloneInputModule>();
#endif
        }
        else
        {
            Debug.Log($"[GameOverManager] EventSystem found: {EventSystem.current.gameObject.name}");
        }
        
        // 2. Check Canvas and its render mode
        Canvas canvas = gameOverPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[GameOverManager] Canvas found: {canvas.name}, RenderMode: {canvas.renderMode}");
            
            // If Canvas uses a camera and that camera is disabled, switch to Overlay
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (canvas.worldCamera == null || !canvas.worldCamera.enabled)
                {
                    Debug.LogWarning("[GameOverManager] Canvas camera is null or disabled! Switching to ScreenSpaceOverlay.");
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }
            
            // 3. Check GraphicRaycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning("[GameOverManager] No GraphicRaycaster on Canvas! Adding one...");
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            else if (!raycaster.enabled)
            {
                Debug.LogWarning("[GameOverManager] GraphicRaycaster was disabled! Enabling...");
                raycaster.enabled = true;
            }
            
            // Ensure canvas is at a high sort order so it's on top
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);
        }
        else
        {
            Debug.LogError("[GameOverManager] No Canvas found in parents of gameOverPanel!");
        }
        
        // 4. Check buttons have Raycast Target on their Images
        if (restartButton != null)
        {
            Image btnImage = restartButton.GetComponent<Image>();
            if (btnImage != null && !btnImage.raycastTarget)
            {
                Debug.LogWarning("[GameOverManager] Restart button Image raycastTarget was false! Enabling...");
                btnImage.raycastTarget = true;
            }
        }
        
        if (quitButton != null)
        {
            Image btnImage = quitButton.GetComponent<Image>();
            if (btnImage != null && !btnImage.raycastTarget)
            {
                Debug.LogWarning("[GameOverManager] Quit button Image raycastTarget was false! Enabling...");
                btnImage.raycastTarget = true;
            }
        }
    }
    
    void OnDestroy()
    {
        // Ensure time scale is reset when script is destroyed
        Time.timeScale = 1f;
    }
}
