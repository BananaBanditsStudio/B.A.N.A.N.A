using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;   // for EventSystem
using TMPro;

public class PauseMenuSetup : MonoBehaviour
{
    [Header("Setup Instructions")]
    [TextArea(5, 10)]
    public string setupInstructions =
        "PAUSE MENU SETUP INSTRUCTIONS:\n\n" +
        "1. Create a Canvas in your scene (this tool will create one if missing)\n" +
        "2. Add the PauseMenu script to an empty GameObject (this tool will create it)\n" +
        "3. This will auto-build a Pause panel with Resume & Quit buttons\n" +
        "4. Press ESC to toggle pause\n";

    [Header("Quick Setup")]
    public bool createBasicUI = false;

    void Start()
    {
        if (createBasicUI)
            CreateBasicPauseMenuUI();
    }

    [ContextMenu("Create Basic Pause Menu UI")]
    public void CreateBasicPauseMenuUI()
    {
        // 0) EventSystem (required for clicks)
        EnsureEventSystem();

        // 1) Canvas (create if missing)
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // 2) Pause menu panel
        GameObject pausePanel = new GameObject("PauseMenuPanel");
        pausePanel.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = pausePanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        // 3) PauseMenuUI (optional animations)
        PauseMenuUI pauseMenuUI = pausePanel.AddComponent<PauseMenuUI>();

        // 4) Title
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

        // 5) Button container
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(pausePanel.transform, false);
        RectTransform buttonRect = buttonContainer.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonRect.sizeDelta = new Vector2(300, 200);
        buttonRect.anchoredPosition = Vector2.zero;

        // 6) Buttons
        GameObject resumeButtonGO = CreateButton("ResumeButton", "Resume", buttonContainer.transform, new Vector2(0, 50));
        Button resumeButton = resumeButtonGO.GetComponent<Button>();

        GameObject quitButtonGO = CreateButton("QuitButton", "Quit to Title", buttonContainer.transform, new Vector2(0, -50));
        Button quitButton = quitButtonGO.GetComponent<Button>();

        // 7) Hook PauseMenuUI references
        pauseMenuUI.pauseMenuPanel = pausePanel;
        pauseMenuUI.resumeButton = resumeButton;
        pauseMenuUI.quitButton = quitButton;
        pauseMenuUI.pauseTitle = titleText;

        // 8) Create PauseMenu object & wire fields
        GameObject pauseMenuGO = new GameObject("PauseMenu");
        PauseMenu pauseMenu = pauseMenuGO.AddComponent<PauseMenu>();
        pauseMenu.pauseMenuUI = pausePanel;
        pauseMenu.resumeButton = resumeButton;
        pauseMenu.quitButton = quitButton;

        // Start hidden; PauseMenu.Start() will also enforce this
        pausePanel.SetActive(false);

        Debug.Log("Basic pause menu UI created. Press ESC to test; the buttons are auto-wired by PauseMenu.Start().");
    }

    GameObject CreateButton(string name, string text, Transform parent, Vector2 position)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(220, 56);
        buttonRect.anchoredPosition = position;

        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button button = buttonGO.AddComponent<Button>();
        // Optional: mouse-only nav
        var nav = new Navigation { mode = Navigation.Mode.None };
        button.navigation = nav;

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

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
