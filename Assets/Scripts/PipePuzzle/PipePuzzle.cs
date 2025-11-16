using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class PipePuzzle : Interactable
{
    [Header("Puzzle Settings")]
    public float timeLimit = 60f;
    public float cellSize = 200f;
    public float spacing = 10f;
    public Canvas canvas;
    
    [Header("Ignored Cells")]
    [Tooltip("Specify which cells to ignore (0-8). Cell index: row * 3 + col. Example: Cell 0 = row0col0, Cell 1 = row0col1")]
    public int[] ignoredCells = new int[0];
    
    [Header("Banana Split Sprites (3x3 grid)")]
    [Tooltip("Assign sprites in order: row1col1, row1col2, row1col3, row2col1, row2col2, row2col3, row3col1, row3col2, row3col3")]
    public Sprite[] puzzleSprites = new Sprite[9];
    
    private GameObject puzzleCanvas;
    private Image[,] puzzleGrid;
    private bool[,] isIgnored; // Track which cells are ignored
    private int[,] rotations; // 0, 90, 180, 270
    private float timeRemaining;
    private bool isActive = false;
    private bool isSolved = false;
    
    // Static reference to check if any puzzle is active
    private static PipePuzzle activePuzzle = null;
    public static bool IsAnyPuzzleActive => activePuzzle != null && activePuzzle.isActive;
    
    private TextMeshProUGUI timerText;
    private Button closeButton;
    
    void Start()
    {
        promptMessage = "Press E to solve banana puzzle";
    }
    
    protected override void Interact()
    {
        OpenPuzzle();
    }
    
    void OpenPuzzle()
    {
        if (isActive) return;
        
        // Ensure the parent canvas has GraphicRaycaster for button interactions
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Ensure EventSystem exists
            if (EventSystem.current == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }
        }
        
        // Create puzzle canvas
        puzzleCanvas = new GameObject("PipePuzzleCanvas");
        puzzleCanvas.transform.SetParent(canvas.transform, false);
        
        RectTransform canvasRect = puzzleCanvas.AddComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        
        CreatePuzzleUI();
        
        // Initialize puzzle
        rotations = new int[3, 3];
        puzzleGrid = new Image[3, 3];
        isIgnored = new bool[3, 3];
        
        // Mark ignored cells
        for (int i = 0; i < ignoredCells.Length; i++)
        {
            int cellIndex = ignoredCells[i];
            if (cellIndex >= 0 && cellIndex < 9)
            {
                int row = cellIndex / 3;
                int col = cellIndex % 3;
                isIgnored[row, col] = true;
            }
        }
        
        // Randomize rotations (solution is all 0, except ignored cells)
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (!isIgnored[row, col])
                {
                    rotations[row, col] = Random.Range(0, 4) * 90;
                }
                else
                {
                    rotations[row, col] = 0; // Ignored cells stay at 0
                }
            }
        }
        
        SetupGrid();
        
        // Start puzzle
        isActive = true;
        isSolved = false;
        timeRemaining = timeLimit;
        activePuzzle = this; // Set as active puzzle
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetPaused(true);
        }
        
        StartCoroutine(PuzzleTimer());
    }
    
    void CreatePuzzleUI()
    {
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(puzzleCanvas.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.85f);
        bgImage.raycastTarget = false; // Don't block clicks on buttons
        
        // Main container (centered)
        GameObject container = new GameObject("Container");
        container.transform.SetParent(puzzleCanvas.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        
        float gridWidth = 3 * cellSize + 2 * spacing;
        float gridHeight = 3 * cellSize + 2 * spacing;
        float containerHeight = gridHeight + 140f; // Extra space for title and timer
        
        containerRect.sizeDelta = new Vector2(gridWidth + 40f, containerHeight);
        containerRect.anchoredPosition = Vector2.zero; // Perfectly centered
        
        Image containerImage = container.AddComponent<Image>();
        containerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        containerImage.raycastTarget = false; // Don't block clicks on buttons
        
        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(container.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(400, 50);
        titleRect.anchoredPosition = new Vector2(0, -15);
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Banana Flow Puzzle";
        titleText.fontSize = 32;
        titleText.color = Color.yellow;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.font = TMPro.TMP_Settings.defaultFontAsset;
        
        // Grid container (centered in main container)
        GameObject gridContainer = new GameObject("GridContainer");
        gridContainer.transform.SetParent(container.transform, false);
        RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
        gridRect.anchoredPosition = Vector2.zero;
        
        // Store grid container for later
        puzzleGridParent = gridRect;
        
        // Timer
        GameObject timerGO = new GameObject("Timer");
        timerGO.transform.SetParent(container.transform, false);
        RectTransform timerRect = timerGO.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0f);
        timerRect.anchorMax = new Vector2(0.5f, 0f);
        timerRect.sizeDelta = new Vector2(300, 40);
        timerRect.anchoredPosition = new Vector2(0, 20);
        timerText = timerGO.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 24;
        timerText.color = Color.white;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.font = TMPro.TMP_Settings.defaultFontAsset;
        
        // Close button
        GameObject closeButtonGO = new GameObject("CloseButton");
        closeButtonGO.transform.SetParent(container.transform, false);
        RectTransform closeRect = closeButtonGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(40, 40);
        closeRect.anchoredPosition = new Vector2(-10, -10);
        closeButton = closeButtonGO.AddComponent<Button>();
        Image closeImage = closeButtonGO.AddComponent<Image>();
        closeImage.color = Color.red;
        GameObject closeTextGO = new GameObject("Text");
        closeTextGO.transform.SetParent(closeButtonGO.transform, false);
        RectTransform closeTextRect = closeTextGO.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 24;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.font = TMPro.TMP_Settings.defaultFontAsset;
        closeButton.onClick.AddListener(ClosePuzzle);
    }
    
    void SetupGrid()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int spriteIndex = row * 3 + col;
                
                // Create cell
                GameObject cell = new GameObject($"Cell_{row}_{col}");
                cell.transform.SetParent(puzzleGridParent, false);
                
                RectTransform cellRect = cell.AddComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = new Vector2(cellSize, cellSize);
                
                // Position cells relative to centered grid container
                float totalWidth = 3 * cellSize + 2 * spacing;
                float totalHeight = 3 * cellSize + 2 * spacing;
                float startX = -totalWidth / 2 + cellSize / 2;
                float startY = totalHeight / 2 - cellSize / 2;
                
                float xPos = startX + col * (cellSize + spacing);
                float yPos = startY - row * (cellSize + spacing);
                cellRect.anchoredPosition = new Vector2(xPos, yPos);
                
                // Button (only if not ignored)
                if (!isIgnored[row, col])
                {
                    Button cellButton = cell.AddComponent<Button>();
                    Image buttonImage = cell.AddComponent<Image>();
                    buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    
                    int r = row;
                    int c = col;
                    cellButton.onClick.AddListener(() => OnCellClicked(r, c));
                }
                else
                {
                    // Make ignored cells slightly darker and non-interactive
                    Image buttonImage = cell.AddComponent<Image>();
                    buttonImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                }
                
                // Pipe sprite
                GameObject pipeGO = new GameObject("Pipe");
                pipeGO.transform.SetParent(cell.transform, false);
                RectTransform pipeRect = pipeGO.AddComponent<RectTransform>();
                pipeRect.anchorMin = Vector2.zero;
                pipeRect.anchorMax = Vector2.one;
                pipeRect.offsetMin = Vector2.zero;
                pipeRect.offsetMax = Vector2.zero;
                
                Image pipeImage = pipeGO.AddComponent<Image>();
                pipeImage.sprite = puzzleSprites[spriteIndex];
                pipeImage.preserveAspect = true;
                pipeImage.raycastTarget = false; // Don't block clicks on button
                
                puzzleGrid[row, col] = pipeImage;
                
                // Apply rotation
                UpdateCellRotation(row, col);
            }
        }
    }
    
    void UpdateCellRotation(int row, int col)
    {
        if (puzzleGrid[row, col] != null)
        {
            puzzleGrid[row, col].transform.rotation = Quaternion.Euler(0, 0, rotations[row, col]);
        }
    }
    
    void OnCellClicked(int row, int col)
    {
        if (isSolved || isIgnored[row, col]) return;
        
        // Rotate 90 degrees
        rotations[row, col] = (rotations[row, col] + 90) % 360;
        UpdateCellRotation(row, col);
        
        CheckSolution();
    }
    
    void Update()
    {
        // ESC key to close puzzle
        if (isActive && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePuzzle();
        }
    }
    
    void CheckSolution()
    {
        bool solved = true;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                // Only check non-ignored cells
                if (!isIgnored[row, col] && rotations[row, col] != 0)
                {
                    solved = false;
                    break;
                }
            }
            if (!solved) break;
        }
        
        if (solved)
        {
            isSolved = true;
            StartCoroutine(DelayedWinPanel());
        }
    }
    
    IEnumerator DelayedWinPanel()
    {
        // Wait 0.5 seconds to show solved state before win message
        yield return new WaitForSecondsRealtime(0.5f);
        ShowWinPanel();
    }
    
    void ShowWinPanel()
    {
        timeRemaining = 0;
        
        GameObject winPanel = new GameObject("WinPanel");
        winPanel.transform.SetParent(puzzleCanvas.transform, false);
        RectTransform winRect = winPanel.AddComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.offsetMin = Vector2.zero;
        winRect.offsetMax = Vector2.zero;
        Image winImage = winPanel.AddComponent<Image>();
        winImage.color = new Color(0, 0.8f, 0, 0.9f);
        
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(winPanel.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600, 200);
        textRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "PUZZLE SOLVED!\n\nGreat job!";
        text.fontSize = 48;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.font = TMPro.TMP_Settings.defaultFontAsset;
        
        // Add close button to win panel
        AddCloseButtonToPanel(winPanel, true);
        
        // Auto-close after delay using coroutine (works with unscaled time)
        StartCoroutine(DelayedClose(2f));
    }
    
    void ShowLosePanel()
    {
        GameObject losePanel = new GameObject("LosePanel");
        losePanel.transform.SetParent(puzzleCanvas.transform, false);
        RectTransform loseRect = losePanel.AddComponent<RectTransform>();
        loseRect.anchorMin = Vector2.zero;
        loseRect.anchorMax = Vector2.one;
        loseRect.offsetMin = Vector2.zero;
        loseRect.offsetMax = Vector2.zero;
        Image loseImage = losePanel.AddComponent<Image>();
        loseImage.color = new Color(0.8f, 0, 0, 0.9f);
        
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(losePanel.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600, 200);
        textRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "TIME'S UP!\n\nTry again!";
        text.fontSize = 48;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.font = TMPro.TMP_Settings.defaultFontAsset;
        
        // Add close button to lose panel
        AddCloseButtonToPanel(losePanel, false);
        
        // Auto-close after delay using coroutine (works with unscaled time)
        StartCoroutine(DelayedClose(2f));
    }
    
    void AddCloseButtonToPanel(GameObject panel, bool isWin)
    {
        GameObject buttonGO = new GameObject("CloseButton");
        buttonGO.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(200, 50);
        buttonRect.anchoredPosition = new Vector2(0, 30);
        
        Button button = buttonGO.AddComponent<Button>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        button.onClick.AddListener(ClosePuzzle);
        
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "Close";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.font = TMPro.TMP_Settings.defaultFontAsset;
    }
    
    IEnumerator DelayedClose(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay && isActive)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (isActive)
        {
            ClosePuzzle();
        }
    }
    
    IEnumerator PuzzleTimer()
    {
        while (timeRemaining > 0 && isActive && !isSolved)
        {
            timeRemaining -= Time.unscaledDeltaTime;
            UpdateTimerDisplay();
            yield return null;
        }
        
        if (timeRemaining <= 0 && !isSolved)
        {
            ShowLosePanel();
        }
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
            
            if (timeRemaining < 10)
                timerText.color = Color.red;
            else if (timeRemaining < 30)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }
    }
    
    void ClosePuzzle()
    {
        isActive = false;
        if (activePuzzle == this)
        {
            activePuzzle = null; // Clear active puzzle reference
        }
        
        if (puzzleCanvas != null)
        {
            Destroy(puzzleCanvas);
        }
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetPaused(false);
        }
        
        if (isSolved && useEvents)
        {
            GetComponent<InteractionEvent>()?.onInteract.Invoke();
        }
    }
    
    private RectTransform puzzleGridParent;
}
