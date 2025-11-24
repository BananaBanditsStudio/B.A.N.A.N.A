using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages game objectives like finding keys, collecting pickups, and reaching locations.
/// This is separate from the tutorial objective system (ObjectiveDisplay/ObjectiveTracker).
/// </summary>
public class GameObjectiveManager : MonoBehaviour
{
    [Header("Objective Text UI")]
    [Tooltip("Text component to display objective text (e.g., 'Find 3 keys (1/3)')")]
    public TextMeshProUGUI objectiveText;

    [Header("Objective UI (Optional - Legacy)")]
    [Tooltip("Optional reference to ObjectiveUI sprite system (if you use it)")]
    public ObjectiveUI objectiveUI; // Optional reference to ObjectiveUI for visual feedback

    [Header("Key Objective Settings")]
    [Tooltip("Number of keys required to complete the first objective")]
    public int requiredKeys = 3;
    
    [Header("Objective Text Settings")]
    [Tooltip("Format for key objective text. {0} = current keys, {1} = required keys")]
    public string keyObjectiveFormat = "Find {1} keys ({0}/{1})";
    
    [Tooltip("Text to show when key objective is complete")]
    public string keyObjectiveCompleteText = "All keys found!";

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Objective tracking
    private int currentObjectiveIndex = 0;
    private int keysCollected = 0;
    private bool keyObjectiveComplete = false;

    // Track collected items/objects for future objectives
    private HashSet<string> collectedItems = new HashSet<string>();
    private HashSet<string> reachedLocations = new HashSet<string>();

    // Events for extensibility
    public System.Action<int> OnKeyCollected;
    public System.Action OnKeyObjectiveComplete;
    public System.Action<int> OnObjectiveComplete; // Passes objective index

    void Start()
    {
        // Auto-find ObjectiveUI if not assigned (optional legacy support)
        if (objectiveUI == null)
        {
            objectiveUI = FindFirstObjectByType<ObjectiveUI>();
        }

        // Initialize key count from PlayerInventory
        keysCollected = PlayerInventory.keyCount;
        
        // Check if objective is already complete (in case keys were collected before manager started)
        if (keysCollected >= requiredKeys && !keyObjectiveComplete)
        {
            CompleteKeyObjective();
        }
        else
        {
            // Update objective text on start
            UpdateObjectiveText();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"GameObjectiveManager: Started. Keys required: {requiredKeys}, Current keys: {keysCollected}");
        }
    }

    void Update()
    {
        // Check key objective if not complete
        if (!keyObjectiveComplete)
        {
            CheckKeyObjective();
        }
    }

    /// <summary>
    /// Checks if the key collection objective is complete.
    /// Monitors PlayerInventory.keyCount for changes.
    /// </summary>
    void CheckKeyObjective()
    {
        int currentKeyCount = PlayerInventory.keyCount;
        
        // Check if key count increased
        if (currentKeyCount > keysCollected)
        {
            keysCollected = currentKeyCount;
            
            if (showDebugLogs)
            {
                Debug.Log($"GameObjectiveManager: Key collected! Progress: {keysCollected}/{requiredKeys}");
            }

            // Notify listeners
            OnKeyCollected?.Invoke(keysCollected);

            // Update objective text
            UpdateObjectiveText();

            // Check if objective is complete
            if (keysCollected >= requiredKeys)
            {
                CompleteKeyObjective();
            }
        }
    }

    /// <summary>
    /// Completes the key finding objective.
    /// </summary>
    void CompleteKeyObjective()
    {
        if (keyObjectiveComplete) return;

        keyObjectiveComplete = true;
        currentObjectiveIndex++;

        if (showDebugLogs)
        {
            Debug.Log($"GameObjectiveManager: Key objective complete! Found {keysCollected} keys.");
        }

        // Notify listeners
        OnKeyObjectiveComplete?.Invoke();
        OnObjectiveComplete?.Invoke(0); // 0 = first objective (key finding)

        // Update objective text
        UpdateObjectiveText();

        // Optional: Notify ObjectiveUI if assigned (legacy support)
        if (objectiveUI != null)
        {
            objectiveUI.MarkObjectiveComplete();
        }
    }

    /// <summary>
    /// Updates the objective text display based on current progress.
    /// </summary>
    void UpdateObjectiveText()
    {
        if (objectiveText == null) return;

        if (keyObjectiveComplete)
        {
            objectiveText.text = keyObjectiveCompleteText;
        }
        else
        {
            // Format: "Find 3 keys (1/3)"
            objectiveText.text = string.Format(keyObjectiveFormat, keysCollected, requiredKeys);
        }
    }

    // ========== Public API for Future Objectives ==========

    /// <summary>
    /// Register that a pickup/item was collected.
    /// Useful for future objectives like "Collect 5 bananas" or "Find all collectibles".
    /// </summary>
    public void RegisterItemCollected(string itemName)
    {
        if (collectedItems.Add(itemName))
        {
            if (showDebugLogs)
            {
                Debug.Log($"GameObjectiveManager: Item collected: {itemName}");
            }
        }
    }

    /// <summary>
    /// Check if a specific item has been collected.
    /// </summary>
    public bool HasCollectedItem(string itemName)
    {
        return collectedItems.Contains(itemName);
    }

    /// <summary>
    /// Get count of unique items collected.
    /// </summary>
    public int GetCollectedItemCount()
    {
        return collectedItems.Count;
    }

    /// <summary>
    /// Register that a location was reached.
    /// Useful for future objectives like "Reach the exit" or "Visit all checkpoints".
    /// </summary>
    public void RegisterLocationReached(string locationName)
    {
        if (reachedLocations.Add(locationName))
        {
            if (showDebugLogs)
            {
                Debug.Log($"GameObjectiveManager: Location reached: {locationName}");
            }
        }
    }

    /// <summary>
    /// Check if a specific location has been reached.
    /// </summary>
    public bool HasReachedLocation(string locationName)
    {
        return reachedLocations.Contains(locationName);
    }

    /// <summary>
    /// Get count of unique locations reached.
    /// </summary>
    public int GetReachedLocationCount()
    {
        return reachedLocations.Count;
    }

    /// <summary>
    /// Manually complete an objective by index.
    /// Useful for custom objectives or testing.
    /// </summary>
    public void CompleteObjective(int objectiveIndex)
    {
        if (showDebugLogs)
        {
            Debug.Log($"GameObjectiveManager: Objective {objectiveIndex} manually completed.");
        }

        OnObjectiveComplete?.Invoke(objectiveIndex);

        // Update objective text
        UpdateObjectiveText();

        // Optional: Notify ObjectiveUI if assigned (legacy support)
        if (objectiveUI != null)
        {
            objectiveUI.MarkObjectiveComplete();
        }
    }

    // ========== Getters for UI/Other Systems ==========

    /// <summary>
    /// Get current key collection progress.
    /// </summary>
    public int GetKeysCollected()
    {
        return keysCollected;
    }

    /// <summary>
    /// Get required number of keys.
    /// </summary>
    public int GetRequiredKeys()
    {
        return requiredKeys;
    }

    /// <summary>
    /// Check if key objective is complete.
    /// </summary>
    public bool IsKeyObjectiveComplete()
    {
        return keyObjectiveComplete;
    }

    /// <summary>
    /// Get current objective index.
    /// </summary>
    public int GetCurrentObjectiveIndex()
    {
        return currentObjectiveIndex;
    }

    /// <summary>
    /// Get key collection progress as a normalized value (0-1).
    /// </summary>
    public float GetKeyProgress()
    {
        if (requiredKeys <= 0) return 1f;
        return Mathf.Clamp01((float)keysCollected / requiredKeys);
    }
}

