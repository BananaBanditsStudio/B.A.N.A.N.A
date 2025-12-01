using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject pauseMenuPanel;   // If null, this GameObject is used
    public Button resumeButton;
    public Button quitButton;
    public TextMeshProUGUI pauseTitle;

    [Header("Background (panel)")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);

    [Header("Button Tint Colors (like your screenshot)")]
    // Strong banana yellow
    public Color btnNormal = new Color(1.00f, 0.96f, 0.35f, 1f);  // #FFF55A-ish
    // Slightly brighter on hover
    public Color btnHighlighted = new Color(1.00f, 0.98f, 0.55f, 1f);  // a touch brighter
    // Warm orange on press
    public Color btnPressed = new Color(0.98f, 0.72f, 0.29f, 1f);  // #F4B84A-ish
    // Selected same as normal
    public Color btnSelected = new Color(1.00f, 0.96f, 0.35f, 1f);
    // Disabled white/gray
    public Color btnDisabled = new Color(0.90f, 0.90f, 0.90f, 0.65f);
    public float btnColorMultiplier = 1f;

    [Header("Text Colors")]
    // Title text face color (yellow like screenshot)
    public Color titleFaceColor = new Color(1.00f, 0.96f, 0.35f, 1f);
    // Button label face color (dark for contrast)
    public Color buttonLabelFaceColor = new Color(0.10f, 0.10f, 0.10f, 1f);

    [Header("Text Outline (like screenshot)")]
    public Color outlineColor = Color.black;
    [Range(0f, 1f)] public float outlineWidth = 0.35f;
    public bool applyOutlineToButtonLabels = true;

    [Header("Animation")]
    public float fadeInDuration = 0.25f;
    public float fadeOutDuration = 0.18f;

    [Header("Title")]
    public string pausedTitle = "POTASSIUM BREAK!";

    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private GameObject panelTarget;
    private bool isInitialized = false;
    private bool showRequested = false; // Track if ShowPauseMenu was called before Start

    void Awake()
    {
        panelTarget = pauseMenuPanel ? pauseMenuPanel : gameObject;

        canvasGroup = panelTarget.GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = panelTarget.AddComponent<CanvasGroup>();

        backgroundImage = panelTarget.GetComponent<Image>();
        if (!backgroundImage) backgroundImage = panelTarget.AddComponent<Image>();
        
        // Set hidden state immediately in Awake (before any Show calls)
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void Start()
    {
        SetupUI();
        isInitialized = true;
        
        // If ShowPauseMenu was called before Start finished, apply it now
        if (showRequested)
        {
            showRequested = false;
            ShowPauseMenu();
        }
    }

    private void SetupUI()
    {
        // Panel look
        backgroundImage.color = backgroundColor;

        // Title look
        if (pauseTitle != null)
        {
            pauseTitle.text = pausedTitle;
            ApplyTMPStyle(pauseTitle, titleFaceColor, outlineColor, outlineWidth);
        }

        // Buttons look
        if (resumeButton) SetupBananaButton(resumeButton, "Resume");
        if (quitButton) SetupBananaButton(quitButton, "Quit");
    }

    private void SetupBananaButton(Button btn, string label)
    {
        // label text
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp)
        {
            tmp.text = label;
            if (applyOutlineToButtonLabels)
                ApplyTMPStyle(tmp, buttonLabelFaceColor, outlineColor, outlineWidth);
            else
                tmp.color = buttonLabelFaceColor;
        }

        // keep sprite bright; tint comes from Button.colors
        if (btn.targetGraphic) btn.targetGraphic.color = Color.white;

        // color-tint states (exact look from your screenshot)
        var c = btn.colors;
        c.normalColor = btnNormal;
        c.highlightedColor = btnHighlighted;
        c.pressedColor = btnPressed;
        c.selectedColor = btnSelected;
        c.disabledColor = btnDisabled;
        c.colorMultiplier = btnColorMultiplier;
        btn.colors = c;

        // make sure the Button is using Color Tint
        if (btn.transition != Selectable.Transition.ColorTint)
            btn.transition = Selectable.Transition.ColorTint;
    }

    /// <summary>
    /// Apply TextMeshPro face color and black outline (like your screenshot).
    /// Uses a material instance per object so you don't mutate shared assets.
    /// </summary>
    private void ApplyTMPStyle(TextMeshProUGUI tmp, Color face, Color outline, float width)
    {
        // Get a unique material instance for this text object
        var mat = tmp.fontMaterial; // accessing fontMaterial creates an instance
        mat.SetColor(ShaderUtilities.ID_FaceColor, face);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, outline);

        // Optional: underlay similar to your screenshot (subtle shadow)
        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        // If you want underlay, uncomment:
        // mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        // mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f,0f,0f,1f));
        // mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        // mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
        // mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
        // mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);

        tmp.fontMaterial = mat; // assign back (ensures instance is used)
        tmp.color = face;       // keep inspector preview consistent
    }

    public void ShowPauseMenu()
    {
        // If Start() hasn't run yet, defer until it does
        if (!isInitialized)
        {
            showRequested = true;
            return;
        }
        
        StopAllCoroutines();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeTo(1f, fadeInDuration));
    }

    public void HidePauseMenu()
    {
        // Immediately block interaction to prevent double-clicks during fade
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // If inactive, just set alpha directly
        if (!gameObject.activeSelf)
        {
            canvasGroup.alpha = 0f;
            return;
        }
        
        StopAllCoroutines();
        StartCoroutine(FadeTo(0f, fadeOutDuration));
    }

    private System.Collections.IEnumerator FadeTo(float target, float duration, System.Action onDone = null)
    {
        float start = canvasGroup.alpha, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
            yield return null;
        }
        canvasGroup.alpha = target;
        onDone?.Invoke();
    }
}
