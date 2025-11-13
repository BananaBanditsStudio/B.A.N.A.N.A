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
    private float originalTimeScale = 1f;

    // Prevent fast double toggles (e.g., click + Esc same frame)
    private float lastToggleTime;
    private const float ToggleCooldown = 0.12f;

    void Awake()
    {
        // Ensure sane initial state
        Time.timeScale = 1f;
        isPaused = false;
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

        // Show UI (no SetActive off/on; use CanvasGroup fade)
        if (pauseMenuUI)
        {
            var ui = pauseMenuUI.GetComponent<PauseMenuUI>();
            if (ui) ui.ShowPauseMenu();
            else pauseMenuUI.SetActive(true);
        }

        if (audioSource && pauseSound) audioSource.PlayOneShot(pauseSound);

        originalTimeScale = 1f;
        Time.timeScale = 0f;

        // Give UI focus and mouse
        var es = EventSystem.current;
        if (es) es.SetSelectedGameObject(null);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        if (pauseMenuUI)
        {
            var ui = pauseMenuUI.GetComponent<PauseMenuUI>();
            if (ui) ui.HidePauseMenu();
            else pauseMenuUI.SetActive(false);
        }

        if (audioSource && resumeSound) audioSource.PlayOneShot(resumeSound);

        Time.timeScale = 1f;

        // Clear any UI selection so Esc is read cleanly next toggle
        var es = EventSystem.current;
        if (es) es.SetSelectedGameObject(null);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitToTitleScreen()
    {
        // Restore timescale first to avoid sticky 0 in next scene
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(titleScreenSceneName);
    }

    public void QuitGame()
    {
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
        // If something disables this, ensure game isn’t left paused
        if (Time.timeScale == 0f) Time.timeScale = 1f;
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
