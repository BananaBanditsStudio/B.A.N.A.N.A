using UnityEngine;

public class LockedDoor : Interactable
{
    [Header("Door Settings")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AudioSource doorSound;
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private int requiredKeyCount = 3;

    [Header("Puzzle Requirement")]
    [SerializeField] private bool requiresPuzzleSolved = true;

    private bool isLocked;
    private bool isOpen = false;
    private int lastKeyCount = -1;
    private bool lastPuzzleState = false;

    private void Start()
    {
        isLocked = startsLocked;
        lastKeyCount = PlayerInventory.keyCount;
        lastPuzzleState = PipePuzzle.IsPuzzleSolved;

        UpdatePromptMessage();
    }

    private void Update()
    {
        int currentKeyCount = PlayerInventory.keyCount;
        bool currentPuzzleState = PipePuzzle.IsPuzzleSolved;

        if (currentKeyCount != lastKeyCount || currentPuzzleState != lastPuzzleState)
        {
            lastKeyCount = currentKeyCount;
            lastPuzzleState = currentPuzzleState;
            UpdatePromptMessage();
        }
    }

    private void UpdatePromptMessage()
    {
        if (isLocked)
        {
            bool hasKeys = PlayerInventory.keyCount >= requiredKeyCount;
            bool puzzleSolved = !requiresPuzzleSolved || PipePuzzle.IsPuzzleSolved;

            if (hasKeys && puzzleSolved)
            {
                promptMessage = "Access granted. Press E to unlock.";
            }
            else if (!hasKeys && !puzzleSolved)
            {
                promptMessage = "Door secured. Find keys and solve the puzzle.";
            }
            else if (!hasKeys)
            {
                promptMessage = "Door secured. Find the keys.";
            }
            else
            {
                promptMessage = "Door secured. Solve the banana puzzle first.";
            }
        }
        else
        {
            promptMessage = isOpen ? "Press E to Close" : "Press E to Open";
        }
    }

    protected override void Interact()
    {
        if (isLocked)
        {
            bool hasKeys = PlayerInventory.keyCount >= requiredKeyCount;
            bool puzzleSolved = !requiresPuzzleSolved || PipePuzzle.IsPuzzleSolved;

            if (hasKeys && puzzleSolved)
            {
                UnlockDoor();
            }
            else
            {
                PlayerUI ui = FindObjectOfType<PlayerUI>();
                if (ui != null)
                {
                    if (!hasKeys && !puzzleSolved)
                        ui.ShowFeedback("Need keys and puzzle solution!");
                    else if (!hasKeys)
                        ui.ShowFeedback("Need more keys!");
                    else
                        ui.ShowFeedback("Solve the puzzle first!");
                }
                UpdatePromptMessage();
                return;
            }
        }

        ToggleDoor();
    }

    private void UnlockDoor()
    {
        isLocked = false;

        PlayerUI ui = FindObjectOfType<PlayerUI>();
        if (ui != null)
            ui.ShowFeedback("Door unlocked!");

        UpdatePromptMessage();
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;

        if (doorAnimator != null)
            doorAnimator.SetBool("isOpen", isOpen);

        if (doorSound != null)
            doorSound.Play();

        UpdatePromptMessage();
    }
}
