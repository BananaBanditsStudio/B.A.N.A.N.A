using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameOverManager2 : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverMenuUI;   // assign the GameOverMenu panel (the one with PauseMenuUI)
    public Button restartButton;
    public Button quitButton;

    [Header("Settings")]
    public string currentSceneName; // Will be set automatically
    public string titleScreenSceneName = "TitleScreen";

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip gameOverSound;
    public AudioClip buttonClickSound;

    [Header("Death Camera")]
    public Camera deathCamera;
    public float deathAnimationDuration = 3f;

    private bool isGameOver = false;
    private PlayerHealth playerHealth;
    private GameStateManager gameStateManager;
    private Camera mainCamera;
    private PauseMenuUI pauseMenuUI; // Reuse the same UI component

    void Awake()
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
        
        // Ensure sane initial state
        Time.timeScale = 1f;
    }

    void Start()
    {
        // Find player health component
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("GameOverManager2: PlayerHealth component not found!");
            return;
        }

        // Get PauseMenuUI component from the game over menu panel
        if (gameOverMenuUI != null)
        {
            pauseMenuUI = gameOverMenuUI.GetComponent<PauseMenuUI>();
            if (pauseMenuUI == null)
            {
                Debug.LogError("[GameOverManager2] gameOverMenuUI GameObject doesn't have PauseMenuUI component! Add it.");
            }
            else
            {
                // Update the title text for game over
                pauseMenuUI.pausedTitle = "GAME OVER";
                
                // Assign buttons to PauseMenuUI (it will style them)
                if (restartButton != null)
                {
                    pauseMenuUI.resumeButton = restartButton; // Reuse resumeButton reference for restart
                }
                
                if (quitButton != null)
                {
                    pauseMenuUI.quitButton = quitButton;
                }
                
                // Set button labels for game over (Restart instead of Resume, Quit stays the same)
                pauseMenuUI.SetButtonLabels("Restart", "Quit");
            }
        }
        else
        {
            Debug.LogError("[GameOverManager2] gameOverMenuUI is NULL! Assign it in the Inspector.");
        }

        // Auto-wire buttons
        if (restartButton)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
        else Debug.LogWarning("[GameOverManager2] restartButton is NULL");

        if (quitButton)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitToTitleScreen);
        }
        else Debug.LogWarning("[GameOverManager2] quitButton is NULL");

        // Initially hide the game over panel
        if (gameOverMenuUI != null)
        {
            gameOverMenuUI.SetActive(false);
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


    public void TriggerGameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        // Play game over sound
        if (audioSource != null && gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }
        
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
        
        // Show game over menu using the same UI system as pause menu
        if (gameOverMenuUI != null)
        {
            gameOverMenuUI.SetActive(true);
            
            if (pauseMenuUI != null)
            {
                pauseMenuUI.ShowPauseMenu();
            }
            
            // IMPORTANT: Force-disable any pause menu that might be lingering
            DisablePauseMenuUI();
            
            // Also close any active puzzle that might be interfering
            CloseAnyActivePuzzle();
            
            Debug.Log("[GameOverManager2] Game over menu shown");
        }
        else
        {
            Debug.LogError("[GameOverManager2] gameOverMenuUI is NULL! Assign it in the Inspector.");
        }
        
        // Ensure cursor is still unlocked (safety check)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
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
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
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
                        Debug.Log("[GameOverManager2] Destroyed active puzzle canvas");
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
        if (pauseUI != null && pauseUI != pauseMenuUI) // Don't disable our own UI
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
            Debug.Log("[GameOverManager2] Disabled other PauseMenuUI to prevent interference");
        }
        
        // Also find the PauseMenu controller and reset its state
        PauseMenu pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (pauseMenu != null && pauseMenu.pauseMenuUI != null)
        {
            pauseMenu.pauseMenuUI.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // Ensure time scale is reset when script is destroyed
        Time.timeScale = 1f;
    }
}

