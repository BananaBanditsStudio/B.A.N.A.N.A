using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    public Button resumeButton;
    public Button quitButton;
    
    [Header("Settings")]
    public string titleScreenSceneName = "TitleScreen";
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pauseSound;
    public AudioClip resumeSound;
    
    private bool isPaused = false;
    private float originalTimeScale;
    private GameStateManager gameStateManager;
    
    void Start()
    {
        // Get or create GameStateManager
        gameStateManager = GameStateManager.Instance;
        if (gameStateManager == null)
        {
            GameObject gameStateGO = new GameObject("GameStateManager");
            gameStateManager = gameStateGO.AddComponent<GameStateManager>();
        }
        
        // Ensure pause menu is hidden at start
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        
        // Set up button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitToTitleScreen);
        }
        
        // Store original time scale
        originalTimeScale = Time.timeScale;
    }
    
    void Update()
    {
        // Check for pause input
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        // Use GameStateManager to properly pause
        if (gameStateManager != null)
        {
            gameStateManager.SetPaused(true);
        }
        
        // Show pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        
        // Play pause sound
        PlaySound(pauseSound);
        
        // Pause audio sources
        PauseAllAudio();
    }
    
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        
        // Use GameStateManager to properly resume
        if (gameStateManager != null)
        {
            gameStateManager.SetPaused(false);
        }
        
        // Hide pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        
        // Play resume sound
        PlaySound(resumeSound);
        
        // Resume audio sources
        ResumeAllAudio();
    }
    
    public void QuitToTitleScreen()
    {
        // Reset game state before scene change
        if (gameStateManager != null)
        {
            gameStateManager.ResetState();
        }
        
        // Resume time scale before scene change
        Time.timeScale = originalTimeScale;
        
        // Load title screen scene
        SceneManager.LoadScene(titleScreenSceneName);
    }
    
    public void QuitGame()
    {
        // Resume time scale before quitting
        Time.timeScale = originalTimeScale;
        
        // Quit the application
        Application.Quit();
        
        // For editor testing
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void PauseAllAudio()
    {
        // Pause all audio sources in the scene
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allAudioSources)
        {
            if (source.isPlaying)
            {
                source.Pause();
            }
        }
    }
    
    private void ResumeAllAudio()
    {
        // Resume all audio sources in the scene
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allAudioSources)
        {
            if (source.clip != null && !source.isPlaying)
            {
                source.UnPause();
            }
        }
    }
    
    // Public getter for pause state (useful for other scripts)
    public bool IsPaused()
    {
        return isPaused;
    }
    
    // Method to toggle pause state (useful for other scripts)
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    void OnDestroy()
    {
        // Ensure time scale is reset when script is destroyed
        Time.timeScale = originalTimeScale;
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // Handle application pause (mobile/background)
        if (pauseStatus && !isPaused)
        {
            PauseGame();
        }
    }
}
