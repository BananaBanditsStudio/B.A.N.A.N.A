using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class GameOverSetup : MonoBehaviour
{
    [Header("Setup Instructions")]
    [TextArea(5, 10)]
    public string setupInstructions = 
        "GAME OVER SCREEN SETUP INSTRUCTIONS:\n\n" +
        "1. Create a Canvas in your scene (if not already present)\n" +
        "2. Add the GameOverManager script to an empty GameObject\n" +
        "3. Create a UI Panel as child of Canvas for the game over screen\n" +
        "4. Add the GameOverSetup script to the panel\n" +
        "5. Create buttons for Restart and Quit\n" +
        "6. Assign references in the GameOverManager script\n" +
        "7. Set the title screen scene name\n\n" +
        "The game over screen will automatically trigger when:\n" +
        "- Player health reaches 0\n" +
        "- Includes fade-in animation\n" +
        "- Pauses the game\n" +
        "- Provides restart and quit options";
    
    [Header("Quick Setup")]
    public bool createBasicGameOverUI = false;
    
    void Start()
    {
        if (createBasicGameOverUI)
        {
            CreateBasicGameOverUI();
        }
    }
    
    [ContextMenu("Create Basic Game Over UI")]
    public void CreateBasicGameOverUI()
    {
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create game over panel
        GameObject gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform and Image
        RectTransform rectTransform = gameOverPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        Image panelImage = gameOverPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f);
        
        // Add CanvasGroup for fade effects
        CanvasGroup canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Create title
        GameObject titleGO = new GameObject("GameOverTitle");
        titleGO.transform.SetParent(gameOverPanel.transform, false);
        
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(600, 120);
        titleRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME OVER";
        titleText.fontSize = 72;
        titleText.color = Color.red;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        // Create message
        GameObject messageGO = new GameObject("GameOverMessage");
        messageGO.transform.SetParent(gameOverPanel.transform, false);
        
        RectTransform messageRect = messageGO.AddComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.sizeDelta = new Vector2(500, 60);
        messageRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI messageText = messageGO.AddComponent<TextMeshProUGUI>();
        messageText.text = "You have been defeated...";
        messageText.fontSize = 24;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Center;
        
        // Create button container
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(gameOverPanel.transform, false);
        
        RectTransform buttonRect = buttonContainer.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.3f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.3f);
        buttonRect.sizeDelta = new Vector2(400, 150);
        buttonRect.anchoredPosition = Vector2.zero;
        
        // Create Restart button
        GameObject restartButtonGO = CreateButton("RestartButton", "Restart Level", buttonContainer.transform, new Vector2(0, 30));
        Button restartButton = restartButtonGO.GetComponent<Button>();
        
        // Create Quit button
        GameObject quitButtonGO = CreateButton("QuitButton", "Quit to Title", buttonContainer.transform, new Vector2(0, -30));
        Button quitButton = quitButtonGO.GetComponent<Button>();
        
        // Create GameOverManager script on a separate GameObject
        GameObject gameOverManagerGO = new GameObject("GameOverManager");
        GameOverManager gameOverManager = gameOverManagerGO.AddComponent<GameOverManager>();
        gameOverManager.gameOverPanel = gameOverPanel;
        gameOverManager.restartButton = restartButton;
        gameOverManager.quitButton = quitButton;
        gameOverManager.gameOverTitle = titleText;
        gameOverManager.gameOverMessage = messageText;
        
        Debug.Log("Basic game over UI created! The GameOverManager will automatically detect when the player dies.");
    }
    
    GameObject CreateButton(string name, string text, Transform parent, Vector2 position)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(250, 50);
        buttonRect.anchoredPosition = position;
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        Button button = buttonGO.AddComponent<Button>();
        
        // Create button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        return buttonGO;
    }
}
