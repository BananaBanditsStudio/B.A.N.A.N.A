using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum StartingObjective
{
    FindKeys,           // Start from beginning
    SolvePuzzle,        // Skip keys
    CollectVaultSecrets,// Skip keys + puzzle
    FindBankVault,      // Skip to bank vault
    StealBanana         // Skip to final objective
}

public class GameObjectiveManager : MonoBehaviour
{
    public static GameObjectiveManager Instance { get; private set; }

    [Header("Starting Point")]
    [Tooltip("Choose which objective to start from (for testing or different game modes)")]
    public StartingObjective startFrom = StartingObjective.FindKeys;

    [Header("Objective Text UI")]
    public TextMeshProUGUI objectiveText;

    [Header("Objective UI (Optional - Legacy)")]
    public ObjectiveUI objectiveUI;

    [Header("Key Objective Settings")]
    public int requiredKeys = 3;
    public string keyObjectiveFormat = "Find {1} keys ({0}/{1})";
    public string keyObjectiveCompleteText = "Keys found!";

    [Header("Puzzle Objective Settings")]
    public string puzzleObjectiveText = "Solve the banana puzzle";
    public string puzzleObjectiveCompleteText = "Puzzle solved!";

    [Header("Vault Secrets Objective Settings")]
    public int requiredKeyCards = 1;
    public string vaultSecretsFormat = "Collect the vault secrets from the locked room ({0}/{1})";
    public string vaultSecretsCompleteText = "Vault secrets collected!";

    [Header("Bank Vault Objective Settings")]
    public string findVaultText = "Find the bank vault";
    public string findVaultCompleteText = "Bank vault found!";

    [Header("Final Objective Settings")]
    public string finalObjectiveText = "Kill all enemies and steal the banana";
    public string finalObjectiveCompleteText = "You win!";

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Objective tracking
    private int currentObjectiveIndex = 0;
    private int keysCollected = 0;
    private int keyCardsCollected = 0;
    private bool keyObjectiveComplete = false;
    private bool puzzleObjectiveComplete = false;
    private bool vaultSecretsObjectiveComplete = false;
    private bool bankVaultObjectiveComplete = false;
    private bool finalObjectiveComplete = false;

    // Static counters for pickups
    public static int keyCardCount = 0;
    public static bool bankVaultFound = false;
    public static bool bananaStolen = false;

    private HashSet<string> collectedItems = new HashSet<string>();
    private HashSet<string> reachedLocations = new HashSet<string>();

    // Events
    public System.Action<int> OnKeyCollected;
    public System.Action OnKeyObjectiveComplete;
    public System.Action OnPuzzleObjectiveComplete;
    public System.Action<int> OnKeyCardCollected;
    public System.Action OnVaultSecretsObjectiveComplete;
    public System.Action OnBankVaultFound;
    public System.Action OnFinalObjectiveComplete;
    public System.Action<int> OnObjectiveComplete;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (objectiveUI == null)
            objectiveUI = FindFirstObjectByType<ObjectiveUI>();

        // Apply starting point skip
        ApplyStartingObjective();

        keysCollected = PlayerInventory.keyCount;
        keyCardsCollected = keyCardCount;

        // Check if objectives are already complete (only for non-skipped objectives)
        if (!keyObjectiveComplete && keysCollected >= requiredKeys)
        {
            CompleteKeyObjective();
        }
        if (!puzzleObjectiveComplete && keyObjectiveComplete && PipePuzzle.IsPuzzleSolved)
        {
            CompletePuzzleObjective();
        }
        if (!vaultSecretsObjectiveComplete && puzzleObjectiveComplete && keyCardsCollected >= requiredKeyCards)
        {
            CompleteVaultSecretsObjective();
        }
        if (!bankVaultObjectiveComplete && vaultSecretsObjectiveComplete && bankVaultFound)
        {
            CompleteBankVaultObjective();
        }

        UpdateObjectiveText();

        if (showDebugLogs)
            Debug.Log($"GameObjectiveManager: Started from '{startFrom}'. Current objective index: {currentObjectiveIndex}");
    }

    void ApplyStartingObjective()
    {
        // Skip objectives based on starting point selection
        switch (startFrom)
        {
            case StartingObjective.FindKeys:
                // Start from beginning - no skips
                break;
                
            case StartingObjective.SolvePuzzle:
                // Skip keys objective
                keyObjectiveComplete = true;
                keysCollected = requiredKeys;
                currentObjectiveIndex = 1;
                break;
                
            case StartingObjective.CollectVaultSecrets:
                // Skip keys + puzzle
                keyObjectiveComplete = true;
                puzzleObjectiveComplete = true;
                keysCollected = requiredKeys;
                currentObjectiveIndex = 2;
                break;
                
            case StartingObjective.FindBankVault:
                // Skip to bank vault objective
                keyObjectiveComplete = true;
                puzzleObjectiveComplete = true;
                vaultSecretsObjectiveComplete = true;
                keysCollected = requiredKeys;
                keyCardsCollected = requiredKeyCards;
                currentObjectiveIndex = 3;
                break;
                
            case StartingObjective.StealBanana:
                // Skip to final objective
                keyObjectiveComplete = true;
                puzzleObjectiveComplete = true;
                vaultSecretsObjectiveComplete = true;
                bankVaultObjectiveComplete = true;
                keysCollected = requiredKeys;
                keyCardsCollected = requiredKeyCards;
                bankVaultFound = true;
                currentObjectiveIndex = 4;
                break;
        }
        
        if (showDebugLogs && startFrom != StartingObjective.FindKeys)
            Debug.Log($"GameObjectiveManager: Skipped to '{startFrom}'");
    }

    void Update()
    {
        if (!keyObjectiveComplete)
        {
            CheckKeyObjective();
        }
        else if (!puzzleObjectiveComplete)
        {
            CheckPuzzleObjective();
        }
        else if (!vaultSecretsObjectiveComplete)
        {
            CheckVaultSecretsObjective();
        }
        else if (!bankVaultObjectiveComplete)
        {
            CheckBankVaultObjective();
        }
        else if (!finalObjectiveComplete)
        {
            CheckFinalObjective();
        }
    }

    void CheckKeyObjective()
    {
        int currentKeyCount = PlayerInventory.keyCount;

        if (currentKeyCount > keysCollected)
        {
            keysCollected = currentKeyCount;

            if (showDebugLogs)
                Debug.Log($"GameObjectiveManager: Key collected! {keysCollected}/{requiredKeys}");

            OnKeyCollected?.Invoke(keysCollected);
            UpdateObjectiveText();

            if (keysCollected >= requiredKeys)
                CompleteKeyObjective();
        }
    }

    void CheckPuzzleObjective()
    {
        if (PipePuzzle.IsPuzzleSolved)
        {
            CompletePuzzleObjective();
        }
    }

    void CheckVaultSecretsObjective()
    {
        if (keyCardCount > keyCardsCollected)
        {
            keyCardsCollected = keyCardCount;

            if (showDebugLogs)
                Debug.Log($"GameObjectiveManager: KeyCard collected! {keyCardsCollected}/{requiredKeyCards}");

            OnKeyCardCollected?.Invoke(keyCardsCollected);
            UpdateObjectiveText();

            if (keyCardsCollected >= requiredKeyCards)
                CompleteVaultSecretsObjective();
        }
    }

    void CheckBankVaultObjective()
    {
        if (bankVaultFound)
        {
            CompleteBankVaultObjective();
        }
    }

    void CheckFinalObjective()
    {
        if (bananaStolen)
        {
            CompleteFinalObjective();
        }
    }

    void CompleteKeyObjective()
    {
        if (keyObjectiveComplete) return;

        keyObjectiveComplete = true;
        currentObjectiveIndex++;

        if (showDebugLogs)
            Debug.Log("GameObjectiveManager: Key objective complete!");

        OnKeyObjectiveComplete?.Invoke();
        OnObjectiveComplete?.Invoke(0);
        UpdateObjectiveText();

        if (objectiveUI != null)
            objectiveUI.MarkObjectiveComplete();
    }

    void CompletePuzzleObjective()
    {
        if (puzzleObjectiveComplete) return;

        puzzleObjectiveComplete = true;
        currentObjectiveIndex++;

        if (showDebugLogs)
            Debug.Log("GameObjectiveManager: Puzzle objective complete!");

        OnPuzzleObjectiveComplete?.Invoke();
        OnObjectiveComplete?.Invoke(1);
        UpdateObjectiveText();
    }

    void CompleteVaultSecretsObjective()
    {
        if (vaultSecretsObjectiveComplete) return;

        vaultSecretsObjectiveComplete = true;
        currentObjectiveIndex++;

        if (showDebugLogs)
            Debug.Log("GameObjectiveManager: Vault secrets objective complete!");

        OnVaultSecretsObjectiveComplete?.Invoke();
        OnObjectiveComplete?.Invoke(2);
        UpdateObjectiveText();
    }

    void CompleteBankVaultObjective()
    {
        if (bankVaultObjectiveComplete) return;

        bankVaultObjectiveComplete = true;
        currentObjectiveIndex++;

        if (showDebugLogs)
            Debug.Log("GameObjectiveManager: Bank vault objective complete!");

        OnBankVaultFound?.Invoke();
        OnObjectiveComplete?.Invoke(3);
        UpdateObjectiveText();
    }

    void CompleteFinalObjective()
    {
        if (finalObjectiveComplete) return;

        finalObjectiveComplete = true;
        currentObjectiveIndex++;

        if (showDebugLogs)
            Debug.Log("GameObjectiveManager: Final objective complete! YOU WIN!");

        OnFinalObjectiveComplete?.Invoke();
        OnObjectiveComplete?.Invoke(4);
        UpdateObjectiveText();
    }

    void UpdateObjectiveText()
    {
        if (objectiveText == null) return;

        if (!keyObjectiveComplete)
        {
            objectiveText.text = string.Format(keyObjectiveFormat, keysCollected, requiredKeys);
        }
        else if (!puzzleObjectiveComplete)
        {
            objectiveText.text = puzzleObjectiveText;
        }
        else if (!vaultSecretsObjectiveComplete)
        {
            objectiveText.text = string.Format(vaultSecretsFormat, keyCardsCollected, requiredKeyCards);
        }
        else if (!bankVaultObjectiveComplete)
        {
            objectiveText.text = findVaultText;
        }
        else if (!finalObjectiveComplete)
        {
            objectiveText.text = finalObjectiveText;
        }
        else
        {
            objectiveText.text = finalObjectiveCompleteText;
        }
    }

    // Public API
    public void RegisterItemCollected(string itemName)
    {
        if (collectedItems.Add(itemName) && showDebugLogs)
            Debug.Log($"GameObjectiveManager: Item collected: {itemName}");
    }

    public bool HasCollectedItem(string itemName) => collectedItems.Contains(itemName);
    public int GetCollectedItemCount() => collectedItems.Count;

    public void RegisterLocationReached(string locationName)
    {
        if (reachedLocations.Add(locationName) && showDebugLogs)
            Debug.Log($"GameObjectiveManager: Location reached: {locationName}");
    }

    public bool HasReachedLocation(string locationName) => reachedLocations.Contains(locationName);
    public int GetReachedLocationCount() => reachedLocations.Count;

    public void CompleteObjective(int objectiveIndex)
    {
        if (showDebugLogs)
            Debug.Log($"GameObjectiveManager: Objective {objectiveIndex} manually completed.");

        OnObjectiveComplete?.Invoke(objectiveIndex);
        UpdateObjectiveText();

        if (objectiveUI != null)
            objectiveUI.MarkObjectiveComplete();
    }

    // Getters
    public int GetKeysCollected() => keysCollected;
    public int GetRequiredKeys() => requiredKeys;
    public bool IsKeyObjectiveComplete() => keyObjectiveComplete;
    public bool IsPuzzleObjectiveComplete() => puzzleObjectiveComplete;
    public bool IsVaultSecretsObjectiveComplete() => vaultSecretsObjectiveComplete;
    public bool IsBankVaultObjectiveComplete() => bankVaultObjectiveComplete;
    public int GetCurrentObjectiveIndex() => currentObjectiveIndex;
    public float GetKeyProgress() => requiredKeys <= 0 ? 1f : Mathf.Clamp01((float)keysCollected / requiredKeys);

    // Static methods for pickups to call
    public static void CollectKeyCard()
    {
        keyCardCount++;
        if (Instance != null && Instance.showDebugLogs)
            Debug.Log($"KeyCard collected! Total: {keyCardCount}");
    }

    public static void FoundBankVault()
    {
        bankVaultFound = true;
        if (Instance != null && Instance.showDebugLogs)
            Debug.Log("Bank vault found!");
    }

    public static void ResetObjectives()
    {
        keyCardCount = 0;
        bankVaultFound = false;
        bananaStolen = false;
    }

    public static void StealBanana()
    {
        bananaStolen = true;
        if (Instance != null && Instance.showDebugLogs)
            Debug.Log("Banana stolen! YOU WIN!");
    }

    public bool IsFinalObjectiveComplete() => finalObjectiveComplete;
}
