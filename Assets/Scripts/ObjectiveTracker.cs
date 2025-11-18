using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectiveTracker : MonoBehaviour
{
    [Header("Objective UI Reference")]
    public ObjectiveUI objectiveUI; // Auto-finds if not assigned

    [Header("Objective States")]
    private bool objective0_WASD_Complete = false;
    private bool objective1_Jump_Complete = false;
    private bool objective2_Crouch_Complete = false;
    private bool objective3_Banana_Complete = false;
    private bool objective4_Getaway_Complete = false;

    // Key tracking for WASD
    private bool wPressed = false;
    private bool aPressed = false;
    private bool sPressed = false;
    private bool dPressed = false;

    void Start()
    {
        // Auto-find ObjectiveUI if not assigned
        if (objectiveUI == null)
        {
            objectiveUI = FindFirstObjectByType<ObjectiveUI>();
        }
    }

    void Update()
    {
        // Only check objectives that aren't complete yet
        if (!objective0_WASD_Complete)
        {
            CheckWASDKeys();
        }

        if (!objective1_Jump_Complete && objective0_WASD_Complete)
        {
            CheckJumpKey();
        }

        if (!objective2_Crouch_Complete && objective1_Jump_Complete)
        {
            CheckCrouchKey();
        }
    }

    void CheckWASDKeys()
    {
        if (Keyboard.current == null) return;

        // Track individual key presses
        if (Keyboard.current.wKey.isPressed && !wPressed)
        {
            wPressed = true;
        }
        if (Keyboard.current.aKey.isPressed && !aPressed)
        {
            aPressed = true;
        }
        if (Keyboard.current.sKey.isPressed && !sPressed)
        {
            sPressed = true;
        }
        if (Keyboard.current.dKey.isPressed && !dPressed)
        {
            dPressed = true;
        }

        // Check if all keys have been pressed
        if (wPressed && aPressed && sPressed && dPressed)
        {
            CompleteObjective(0);
        }
    }

    void CheckJumpKey()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.isPressed)
        {
            CompleteObjective(1);
        }
    }

    void CheckCrouchKey()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.cKey.isPressed)
        {
            CompleteObjective(2);
        }
    }

    void CompleteObjective(int objectiveIndex)
    {
        switch (objectiveIndex)
        {
            case 0:
                if (!objective0_WASD_Complete)
                {
                    objective0_WASD_Complete = true;
                    NotifyObjectiveUI();
                }
                break;
            case 1:
                if (!objective1_Jump_Complete)
                {
                    objective1_Jump_Complete = true;
                    NotifyObjectiveUI();
                }
                break;
            case 2:
                if (!objective2_Crouch_Complete)
                {
                    objective2_Crouch_Complete = true;
                    NotifyObjectiveUI();
                }
                break;
            case 3:
                if (!objective3_Banana_Complete)
                {
                    objective3_Banana_Complete = true;
                    NotifyObjectiveUI();
                }
                break;
            case 4:
                if (!objective4_Getaway_Complete)
                {
                    objective4_Getaway_Complete = true;
                    NotifyObjectiveUI();
                }
                break;
        }
    }

    void NotifyObjectiveUI()
    {
        if (objectiveUI != null)
        {
            objectiveUI.MarkObjectiveComplete();
        }
    }

    // Public methods for other scripts to call
    public void CompleteBananaObjective()
    {
        CompleteObjective(3);
    }

    public void CompleteGetawayObjective()
    {
        CompleteObjective(4);
    }
}

