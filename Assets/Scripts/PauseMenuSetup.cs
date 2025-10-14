using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PauseMenuSetup : MonoBehaviour
{
    [Header("Setup Instructions")]
    [TextArea(5, 10)]
    public string setupInstructions = 
        "PAUSE MENU SETUP INSTRUCTIONS:\n\n" +
        "1. Create a Canvas in your scene\n" +
        "2. Add the PauseMenu script to an empty GameObject\n" +
        "3. Create a UI Panel as child of Canvas for the pause menu\n" +
        "4. Add the PauseMenuUI script to the panel\n" +
        "5. Create buttons for Resume and Quit\n" +
        "6. Assign references in the PauseMenu script\n" +
        "7. Set the title screen scene name\n\n" +
        "The pause menu will automatically handle:\n" +
        "- ESC key to pause/resume\n" +
        "- Time scale management\n" +
        "- Audio pausing/resuming\n" +
        "- Cursor lock management";
    
    [Header("Quick Setup")]
    public bool createBasicUI = false;
    
    void Start()
    {
        if (createBasicUI)
        {
            CreateBasicPauseMenuUI();
        }
    }
    
    [ContextMenu("Create Basic Pause Menu UI")]
    public void CreateBasicPauseMenuUI()
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
        
        // Create pause menu panel
        GameObject pausePanel = new GameObject("PauseMenuPanel");
        pausePanel.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform and Image
        RectTransform rectTransform = pausePanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // Add PauseMenuUI script
        PauseMenuUI pauseMenuUI = pausePanel.AddComponent<PauseMenuUI>();
        
        // Create title
        GameObject titleGO = new GameObject("PauseTitle");
        titleGO.transform.SetParent(pausePanel.transform, false);
        
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(400, 100);
        titleRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        
        // Create button container
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(pausePanel.transform, false);
        
        RectTransform buttonRect = buttonContainer.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonRect.sizeDelta = new Vector2(300, 200);
        buttonRect.anchoredPosition = Vector2.zero;
        
        // Create Resume button
        GameObject resumeButtonGO = CreateButton("ResumeButton", "Resume", buttonContainer.transform, new Vector2(0, 50));
        Button resumeButton = resumeButtonGO.GetComponent<Button>();
        
        // Create Quit button
        GameObject quitButtonGO = CreateButton("QuitButton", "Quit to Title", buttonContainer.transform, new Vector2(0, -50));
        Button quitButton = quitButtonGO.GetComponent<Button>();
        
        // Assign references to PauseMenuUI
        pauseMenuUI.pauseMenuPanel = pausePanel;
        pauseMenuUI.resumeButton = resumeButton;
        pauseMenuUI.quitButton = quitButton;
        pauseMenuUI.pauseTitle = titleText;
        
        // Create PauseMenu script on a separate GameObject
        GameObject pauseMenuGO = new GameObject("PauseMenu");
        PauseMenu pauseMenu = pauseMenuGO.AddComponent<PauseMenu>();
        pauseMenu.pauseMenuUI = pausePanel;
        pauseMenu.resumeButton = resumeButton;
        pauseMenu.quitButton = quitButton;
        
        Debug.Log("Basic pause menu UI created! Check the PauseMenu script for any remaining setup.");
    }
    
    GameObject CreateButton(string name, string text, Transform parent, Vector2 position)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 50);
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
