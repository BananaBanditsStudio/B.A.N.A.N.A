using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button quitButton;
    public TextMeshProUGUI pauseTitle;
    
    [Header("Styling")]
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    public Color buttonColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    public Color textColor = Color.white;
    
    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.2f;
    
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private PauseMenu pauseMenuScript;
    
    void Start()
    {
        // Get or add components
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }
        
        // Find the pause menu script
        pauseMenuScript = FindFirstObjectByType<PauseMenu>();
        
        // Set up UI
        SetupUI();
        
        // Initially hide the menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    
    void SetupUI()
    {
        // Set up background
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }
        
        // Set up title
        if (pauseTitle != null)
        {
            pauseTitle.text = "PAUSED";
            pauseTitle.color = textColor;
        }
        
        // Set up buttons
        if (resumeButton != null)
        {
            SetupButton(resumeButton, "Resume");
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
            buttonText.color = textColor;
        }
        
        // Set button colors
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(buttonColor.r + 0.1f, buttonColor.g + 0.1f, buttonColor.b + 0.1f, buttonColor.a);
        colors.pressedColor = new Color(buttonColor.r - 0.1f, buttonColor.g - 0.1f, buttonColor.b - 0.1f, buttonColor.a);
        button.colors = colors;
    }
    
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // Animate fade in
        StartCoroutine(FadeIn());
    }
    
    public void HidePauseMenu()
    {
        // Animate fade out
        StartCoroutine(FadeOut());
    }
    
    System.Collections.IEnumerator FadeIn()
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
    
    System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }
    
    // Public methods for button events
    public void OnResumeClicked()
    {
        if (pauseMenuScript != null)
        {
            pauseMenuScript.ResumeGame();
        }
    }
    
    public void OnQuitClicked()
    {
        if (pauseMenuScript != null)
        {
            pauseMenuScript.QuitToTitleScreen();
        }
    }
}
