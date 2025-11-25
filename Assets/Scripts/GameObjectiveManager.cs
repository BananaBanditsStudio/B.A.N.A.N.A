using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameObjectiveManager : MonoBehaviour
{
    public static GameObjectiveManager Instance { get; private set; }

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

    // Static counters for pickups
    public static int keyCardCount = 0;
    public static bool bankVaultFound = false;

    private HashSet<string> collectedItems = new HashSet<string>();
    private HashSet<string> reachedLocations = new HashSet<string>();

    // Events
    public System.Action<int> OnKeyCollected;
    public System.Action OnKeyObjectiveComplete;
    public System.Action OnPuzzleObjectiveComplete;
    public System.Action<int> OnKeyCardCollected;
    public System.Action OnVaultSecretsObjectiveComplete;
    public System.Action OnBankVaultFound;
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

        keysCollected = PlayerInventory.keyCount;
        keyCardsCollected = keyCardCount;

        // Check if objectives are already complete
        if (keysCollected >= requiredKeys && !keyObjectiveComplete)
        {
            CompleteKeyObjective();
        }
        if (PipePuzzle.IsPuzzleSolved && !puzzleObjectiveComplete && keyObjectiveComplete)
        {
            CompletePuzzleObjective();
        }
        if (keyCardsCollected >= requiredKeyCards && !vaultSecretsObjectiveComplete && puzzleObjectiveComplete)
        {
            CompleteVaultSecretsObjective();
        }
        if (bankVaultFound && !bankVaultObjectiveComplete && vaultSecretsObjectiveComplete)
        {
            CompleteBankVaultObjective();
        }

        UpdateObjectiveText();

        if (showDebugLogs)
            Debug.Log($"GameObjectiveManager: Started. Keys: {keysCollected}/{requiredKeys}");
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
        else
        {
            objectiveText.text = "All objectives complete!";
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
    }
}
