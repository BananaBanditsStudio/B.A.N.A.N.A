using UnityEngine;
using TMPro;
using System.Collections;

public class ObjectiveDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI objectiveText;
    public CanvasGroup canvasGroup;  // For fade animation
    public ObjectiveUI objectiveUI;  // Reference to the new ObjectiveUI system (optional)

    [Header("Objective Settings")]
    [TextArea] public string[] objectives = {
        "Use WASD keys to move around",          // 0
        "Press SPACE to jump",                   // 1
        "Press CTRL to crouch",                  // 2
        "Press B to dash",                       // 3
        "Press M to open and close map",         // 4
        "Steal the banana from the playground",  // 5 (event-only)
        "Go to the escape car",                  // 6 (event-only)
        "All Objectives Complete!"               // 7
    };
    public float fadeDuration = 1f;
    public float displayDuration = 3f;

    private int currentObjectiveIndex = 0;
    private bool showing = false;
    private float objectiveStartTime = 0f;
    
    // Key tracking
    private bool wPressed = false;
    private bool aPressed = false;
    private bool sPressed = false;
    private bool dPressed = false;
    private bool spacePressed = false;
    private bool cPressed = false;
    private bool bPressed = false;
    private bool mPressed = false;

    void Start()
    {
        canvasGroup.alpha = 0f;
        ShowNextObjective();
    }
    
    void Update()
    {
        // Check keys based on current objective (first 5 objectives are key-based)
        if (showing)
        {
            switch (currentObjectiveIndex)
            {
                case 0: // WASD objective
                    CheckWASDKeys();
                    break;
                case 1: // Jump objective
                    CheckJumpKey();
                    break;
                case 2: // Crouch objective
                    CheckCrouchKey();
                    break;
                case 3: // Dash objective
                    CheckDashKey();
                    break;
                case 4: // Map objective
                    CheckMapKey();
                    break;
                // 5: Steal banana (completed via event)
                // 6: Go to escape car (completed via event)
                // 7: "All Objectives Complete!" auto-completes after delay
            }
        }
        
        // Auto-complete final objective ("All Objectives Complete!") after a delay
        if (currentObjectiveIndex == 7 && showing && Time.time - objectiveStartTime >= displayDuration)
        {
            Debug.Log($"Auto-completing final objective after {displayDuration} seconds");
            CompleteObjective();
        }
    }
    
    void CheckWASDKeys()
    {
        // Check for WASD key presses using Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            bool wKey = UnityEngine.InputSystem.Keyboard.current.wKey.isPressed;
            bool aKey = UnityEngine.InputSystem.Keyboard.current.aKey.isPressed;
            bool sKey = UnityEngine.InputSystem.Keyboard.current.sKey.isPressed;
            bool dKey = UnityEngine.InputSystem.Keyboard.current.dKey.isPressed;
            
            // Track individual key presses
            if (wKey && !wPressed)
            {
                wPressed = true;
                Debug.Log("W key pressed");
            }
            if (aKey && !aPressed)
            {
                aPressed = true;
                Debug.Log("A key pressed");
            }
            if (sKey && !sPressed)
            {
                sPressed = true;
                Debug.Log("S key pressed");
            }
            if (dKey && !dPressed)
            {
                dPressed = true;
                Debug.Log("D key pressed");
            }
            
            // Check if all keys have been pressed
            if (wPressed && aPressed && sPressed && dPressed)
            {
                Debug.Log("All WASD keys pressed! Objective completed.");
                CompleteObjective();
            }
        }
    }
    
    void CheckJumpKey()
    {
        // Check for SPACE key press using Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            bool spaceKey = UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;
            
            if (spaceKey && !spacePressed)
            {
                spacePressed = true;
                Debug.Log("SPACE key pressed - Jump objective completed!");
                CompleteObjective();
            }
        }
    }
    
    void CheckCrouchKey()
    {
        // Check for CTRL key press using Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            bool leftCtrlKey = UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed;
            bool rightCtrlKey = UnityEngine.InputSystem.Keyboard.current.rightCtrlKey.isPressed;
            bool ctrlKey = leftCtrlKey || rightCtrlKey;
            
            if (ctrlKey && !cPressed)
            {
                cPressed = true;
                Debug.Log("CTRL key pressed - Crouch objective completed!");
                CompleteObjective();
            }
        }
    }
    
    void CheckDashKey()
    {
        // Check for B key press using Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            bool bKey = UnityEngine.InputSystem.Keyboard.current.bKey.isPressed;
            
            if (bKey && !bPressed)
            {
                bPressed = true;
                Debug.Log("B key pressed - Dash objective completed!");
                CompleteObjective();
            }
        }
    }
    
    void CheckMapKey()
    {
        // Check for M key press using Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            bool mKey = UnityEngine.InputSystem.Keyboard.current.mKey.isPressed;
            
            if (mKey && !mPressed)
            {
                mPressed = true;
                Debug.Log("M key pressed - Map objective completed!");
                CompleteObjective();
            }
        }
    }

    /// <summary>
    /// Called from an EventOnlyInteractable when the player steals the banana.
    /// </summary>
    public void CompleteStealBananaObjective()
    {
        if (currentObjectiveIndex == 5)
        {
            Debug.Log("Steal Banana objective completed via event.");
            CompleteObjective();
        }
    }

    /// <summary>
    /// Called from an EventOnlyInteractable when the player reaches the escape car.
    /// </summary>
    public void CompleteEscapeCarObjective()
    {
        if (currentObjectiveIndex == 6)
        {
            Debug.Log("Escape Car objective completed via event.");
            CompleteObjective();
        }
    }

    public void CompleteObjective()
    {
        if (!showing) return;
        
        // Notify ObjectiveUI if assigned for all real objectives (exclude final \"All Objectives Complete!\" line)
        if (objectiveUI != null && currentObjectiveIndex < objectives.Length - 1)
        {
            objectiveUI.MarkObjectiveComplete();
        }
        
        StartCoroutine(SwitchObjective());
    }

    private IEnumerator SwitchObjective()
    {
        showing = false;
        yield return FadeOut();

        currentObjectiveIndex++;
        ShowNextObjective();
    }

    private void ShowNextObjective()
    {
        if (currentObjectiveIndex >= objectives.Length)
        {
            objectiveText.text = "All objectives complete!";
        }
        else
        {
            objectiveText.text = objectives[currentObjectiveIndex];
            objectiveStartTime = Time.time; // Track when this objective started
        }

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1;
        showing = true;
    }

    private IEnumerator FadeOut()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}
