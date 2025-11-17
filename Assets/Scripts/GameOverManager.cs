using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
    
    private bool isGameOver = false;
    private PlayerHealth playerHealth;
    private CanvasGroup canvasGroup;
    private GameStateManager gameStateManager;
    
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
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitToTitleScreen);
        }
        
        // Get or add canvas group for fade effects
        canvasGroup = gameOverPanel?.GetComponent<CanvasGroup>();
        if (canvasGroup == null && gameOverPanel != null)
        {
            canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }
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
        // Wait a moment before showing game over
        yield return new WaitForSeconds(delayBeforeShow);
        
        // Use GameStateManager to properly set game over
        if (gameStateManager != null)
        {
            gameStateManager.SetGameOver(true);
        }
        
        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Fade in the game over screen
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeInGameOver());
        }
        
        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    System.Collections.IEnumerator FadeInGameOver()
    {
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
    
    void OnDestroy()
    {
        // Ensure time scale is reset when script is destroyed
        Time.timeScale = 1f;
    }
}
