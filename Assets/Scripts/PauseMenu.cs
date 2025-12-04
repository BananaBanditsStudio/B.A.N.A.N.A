using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;   // assign the PauseMenu panel (the one with PauseMenuUI)
    public Button resumeButton;
    public Button quitButton;

    [Header("Settings")]
    public string titleScreenSceneName = "TitleScreen";
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip pauseSound;
    public AudioClip resumeSound;

    [SerializeField] private bool isPaused = false;
    private GameStateManager gameStateManager;

    // Prevent fast double toggles (e.g., click + Esc same frame)
    private float lastToggleTime;
    private const float ToggleCooldown = 0.12f;

    void Awake()
    {
        // Get or create GameStateManager
        gameStateManager = GameStateManager.Instance;
        if (gameStateManager == null)
        {
            GameObject gameStateGO = new GameObject("GameStateManager");
            gameStateManager = gameStateGO.AddComponent<GameStateManager>();
        }
        
        // Don't sync with GameStateManager - we manage our own pause state independently
        // This ensures the pause menu only shows when Escape is pressed, not when other systems pause
        isPaused = false;
        
        // Ensure sane initial state
        Time.timeScale = 1f;
    }

    void Start()
    {
        // Auto-wire buttons (keep Button OnClick lists EMPTY)
        if (resumeButton)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
        else Debug.LogWarning("[PauseMenu] resumeButton is NULL");

        if (quitButton)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitToTitleScreen);
        }
        else Debug.LogWarning("[PauseMenu] quitButton is NULL");

        // Make sure the pause panel can receive input and draw on top
        // EnsureTopCanvasAndRaycaster();
    }

    void Update()
    {
        // Block pause menu while puzzle is active
        if (PipePuzzle.IsAnyPuzzleActive)
        {
            if (isPaused)
            {
                // Puzzle opened while pause menu was active - close it
                ResumeGame();
            }
            return; // Don't process pause input while puzzle is active
        }
        
        // Block pause menu during game over - don't interfere with game over UI
        if (gameStateManager != null && gameStateManager.isGameOver)
        {
            if (isPaused)
            {
                // Game over triggered while pause menu was active - hide pause menu
                // but don't call ResumeGame() which would lock the cursor
                isPaused = false;
                if (pauseMenuUI)
                {
                    var ui = pauseMenuUI.GetComponent<PauseMenuUI>();
                    if (ui) ui.HidePauseMenu();
                    else pauseMenuUI.SetActive(false);
                }
            }
            return; // Don't process pause input during game over
        }
        
        // Toggle cooldown to prevent double-toggles
        if (Time.unscaledTime - lastToggleTime < ToggleCooldown) return;

        if (Input.GetKeyDown(pauseKey))
        {
            lastToggleTime = Time.unscaledTime;
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        // Pause the game
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show UI via CanvasGroup fade (pauseMenuUI should always be active)
        if (pauseMenuUI)
        {
            pauseMenuUI.SetActive(true); // Ensure active once
            
            var ui = pauseMenuUI.GetComponent<PauseMenuUI>();
            if (ui)
            {
                ui.ShowPauseMenu();
            }
        }

        if (audioSource && pauseSound) audioSource.PlayOneShot(pauseSound);

        // Clear UI selection so keyboard nav starts fresh
        var es = EventSystem.current;
        if (es) es.SetSelectedGameObject(resumeButton ? resumeButton.gameObject : null);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        // Only restore normal gameplay state if NOT in game over
        // This prevents re-locking the cursor when game over is active
        if (gameStateManager == null || !gameStateManager.isGameOver)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Hide UI via CanvasGroup fade
        if (pauseMenuUI)
        {
            var ui = pauseMenuUI.GetComponent<PauseMenuUI>();
            if (ui)
            {
                ui.HidePauseMenu();
            }
            else
            {
                pauseMenuUI.SetActive(false);
            }
        }

        if (audioSource && resumeSound) audioSource.PlayOneShot(resumeSound);

        // Clear UI selection
        var es = EventSystem.current;
        if (es) es.SetSelectedGameObject(null);
    }

    public void QuitToTitleScreen()
    {
        // Restore timescale before loading scene
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(titleScreenSceneName);
    }

    public void QuitGame()
    {
        // Restore timescale before quitting
        Time.timeScale = 1f;
        isPaused = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDisable()
    {
        // If something disables this, ensure game isn't left paused
        if (isPaused && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
        isPaused = false;
    }

    private void EnsureTopCanvasAndRaycaster()
    {
        if (!pauseMenuUI) return;

        var canvas = pauseMenuUI.GetComponentInParent<Canvas>();
        if (!canvas)
        {
            canvas = pauseMenuUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Make sure this pause canvas is on top
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        // Raycaster so UI gets clicks
        var ray = pauseMenuUI.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>();
        if (!ray) pauseMenuUI.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }
}
