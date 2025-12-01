using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Game Win Screen - Shows victory UI with game stats.
/// Call TriggerWin() from an interactable's OnInteract event to show the win screen.
/// </summary>
public class GameWinScreen : MonoBehaviour
{
    [Header("Settings")]
    public string titleScreenSceneName = "TitleScreen";
    public Canvas targetCanvas; // Optional - if null, creates its own canvas
    
    [Header("UI Colors")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.9f);
    public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color titleColor = new Color(1f, 0.85f, 0f, 1f); // Gold
    public Color textColor = Color.white;
    public Color buttonColor = new Color(1f, 0.85f, 0f, 1f); // Gold
    public Color buttonTextColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    
    [Header("Animation")]
    public float delayBeforeWinScreen = 1.5f; // Time to show "objective complete" before win screen
    public float fadeInDuration = 0.5f;
    
    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip winSound;
    
    private GameObject winScreenUI;
    private CanvasGroup canvasGroup;
    private bool hasTriggered = false;
    
    // Singleton for easy access
    public static GameWinScreen Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    /// <summary>
    /// Call this to trigger the win screen (from interactable OnInteract event)
    /// </summary>
    public void TriggerWin()
    {
        if (hasTriggered) return;
        hasTriggered = true;
        
        // Mark banana as stolen for objective system (shows "objective complete" in UI)
        GameObjectiveManager.StealBanana();
        
        // Start the delayed win sequence
        StartCoroutine(DelayedWinSequence());
    }
    
    IEnumerator DelayedWinSequence()
    {
        // Wait for player to see "objective complete" message
        yield return new WaitForSeconds(delayBeforeWinScreen);
        
        // Pause the game
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Play win sound
        if (audioSource != null && winSound != null)
        {
            audioSource.PlayOneShot(winSound);
        }
        
        // Create the win screen UI
        CreateWinScreenUI();
        
        // Fade in
        StartCoroutine(FadeIn());
    }
    
    void CreateWinScreenUI()
    {
        // Create canvas if needed
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("WinScreenCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000; // On top of everything
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create main container
        winScreenUI = new GameObject("WinScreen");
        winScreenUI.transform.SetParent(canvas.transform, false);
        
        RectTransform winRect = winScreenUI.AddComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.offsetMin = Vector2.zero;
        winRect.offsetMax = Vector2.zero;
        
        canvasGroup = winScreenUI.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        // Background
        Image bgImage = winScreenUI.AddComponent<Image>();
        bgImage.color = backgroundColor;
        
        // Center panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(winScreenUI.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 450);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;
        
        // Title - "YOU WIN!"
        CreateText(panel.transform, "Title", "YOU WIN!", 56, titleColor, new Vector2(0, 150));
        
        // Subtitle
        CreateText(panel.transform, "Subtitle", "The banana is yours!", 24, textColor, new Vector2(0, 80));
        
        // Stats section
        float statsY = 20f;
        
        // Kill count
        int kills = GameStatsUI.KillCount;
        CreateText(panel.transform, "KillsLabel", "Enemies Eliminated:", 20, textColor, new Vector2(-80, statsY));
        CreateText(panel.transform, "KillsValue", kills.ToString(), 28, titleColor, new Vector2(100, statsY));
        
        // Time (get from GameStatsUI if available, or calculate)
        statsY -= 50f;
        string timeText = GetElapsedTimeText();
        CreateText(panel.transform, "TimeLabel", "Time:", 20, textColor, new Vector2(-80, statsY));
        CreateText(panel.transform, "TimeValue", timeText, 28, titleColor, new Vector2(100, statsY));
        
        // Buttons
        float buttonY = -120f;
        
        // Play Again button
        CreateButton(panel.transform, "PlayAgain", "Play Again", buttonY, () => {
            Time.timeScale = 1f;
            GameStatsUI.ResetStats();
            GameObjectiveManager.ResetObjectives();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
        
        // Main Menu button
        buttonY -= 60f;
        CreateButton(panel.transform, "MainMenu", "Main Menu", buttonY, () => {
            Time.timeScale = 1f;
            GameStatsUI.ResetStats();
            GameObjectiveManager.ResetObjectives();
            SceneManager.LoadScene(titleScreenSceneName);
        });
    }
    
    void CreateText(Transform parent, string name, string content, int fontSize, Color color, Vector2 position)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        
        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 60);
        rect.anchoredPosition = position;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = TMP_Settings.defaultFontAsset;
    }
    
    void CreateButton(Transform parent, string name, string label, float yPos, System.Action onClick)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, 50);
        rect.anchoredPosition = new Vector2(0, yPos);
        
        Image btnImage = buttonGO.AddComponent<Image>();
        btnImage.color = buttonColor;
        
        Button btn = buttonGO.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        // Button colors
        ColorBlock colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(buttonColor.r * 1.1f, buttonColor.g * 1.1f, buttonColor.b * 1.1f, 1f);
        colors.pressedColor = new Color(buttonColor.r * 0.8f, buttonColor.g * 0.8f, buttonColor.b * 0.8f, 1f);
        btn.colors = colors;
        
        btn.onClick.AddListener(() => onClick());
        
        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = buttonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = TMP_Settings.defaultFontAsset;
    }
    
    string GetElapsedTimeText()
    {
        // Try to find GameStatsUI to get elapsed time
        GameStatsUI statsUI = FindFirstObjectByType<GameStatsUI>();
        if (statsUI != null && statsUI.timerText != null)
        {
            return statsUI.timerText.text;
        }
        
        // Fallback - return placeholder
        return "--:--";
    }
    
    IEnumerator FadeIn()
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
    
    /// <summary>
    /// Static method to trigger win from anywhere
    /// </summary>
    public static void Win()
    {
        if (Instance != null)
        {
            Instance.TriggerWin();
        }
        else
        {
            Debug.LogWarning("GameWinScreen.Win() called but no instance exists! Add GameWinScreen to your scene.");
        }
    }
}

