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
        EnsureTopCanvasAndRaycaster();
    }

    void Update()
    {
        // If puzzle is active, disable pause menu canvas to prevent blocking puzzle UI
        if (PipePuzzle.IsAnyPuzzleActive)
        {
            if (isPaused)
            {
                // Puzzle opened while pause menu was active - close it
                ResumeGame();
            }
            
            // Disable pause menu canvas to prevent it from blocking puzzle UI
            if (pauseMenuUI != null)
            {
                var canvas = pauseMenuUI.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.enabled = false; // Disable canvas to prevent blocking
                }
            }
            return;
        }
        
        // Re-enable pause menu canvas when puzzle is not active
        if (pauseMenuUI != null)
        {
            var canvas = pauseMenuUI.GetComponentInParent<Canvas>();
            if (canvas != null && !canvas.enabled)
            {
                canvas.enabled = true; // Re-enable when puzzle closes
            }
        }
        
        // Only respond to Escape key - don't sync with other pause systems
        // This ensures the pause menu only shows when user presses Escape
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

        // Pause the game - but don't interfere with GameStateManager
        // We manage our own pause state independently
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show UI (no SetActive off/on; use CanvasGroup fade)
        if (pauseMenuUI)
        {
            // Ensure GameObject is active before trying to start coroutine
            if (!pauseMenuUI.activeSelf)
            {
                pauseMenuUI.SetActive(true);
            }
            
            var ui = pauseMenuUI.GetComponent<PauseMenuUI>();
            if (ui && pauseMenuUI.activeSelf)
            {
                ui.ShowPauseMenu();
            }
            else
            {
                pauseMenuUI.SetActive(true);
            }
        }

        if (audioSource && pauseSound) audioSource.PlayOneShot(pauseSound);

        // Give UI focus and mouse
        var es = EventSystem.current;
        if (es) es.SetSelectedGameObject(null);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        // Resume the game - manage our own pause state independently
        // But don't change time scale if puzzle is active (puzzle manages it)
        if (!PipePuzzle.IsAnyPuzzleActive)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (pauseMenuUI)
        {
            // Ensure GameObject is active before trying to start coroutine
            if (!pauseMenuUI.activeSelf)
            {
                pauseMenuUI.SetActive(true);
            }
            
            var ui = pauseMenuUI.GetComponent<PauseMenuUI>();
            if (ui && pauseMenuUI.activeSelf)
            {
                ui.HidePauseMenu();
            }
            else
            {
                pauseMenuUI.SetActive(false);
            }
        }

        if (audioSource && resumeSound) audioSource.PlayOneShot(resumeSound);

        // Clear any UI selection so Esc is read cleanly next toggle
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
