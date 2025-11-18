using UnityEngine;

public class LockedDoor : Interactable
{
    [Header("Door Settings")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AudioSource doorSound; // optional
    [SerializeField] private bool startsLocked = true; // toggle in Inspector
    [SerializeField] private int requiredKeyCount = 3;

    private bool isLocked;
    private bool isOpen = false;
    private int lastKeyCount = -1; // Track key count to only update when it changes

    private void Start()
    {
        isLocked = startsLocked;
        lastKeyCount = PlayerInventory.keyCount;

        // Initialize prompt message at start
        UpdatePromptMessage();
    }

    private void Update()
    {
        // Only update prompt message when key count changes (performance optimization)
        int currentKeyCount = PlayerInventory.keyCount;
        if (currentKeyCount != lastKeyCount)
        {
            lastKeyCount = currentKeyCount;
            UpdatePromptMessage();
        }
    }

    private void UpdatePromptMessage()
    {
        if (isLocked && PlayerInventory.keyCount >= requiredKeyCount)
        {
            promptMessage = "Access granted. Press E to unlock.";
        }
        else if (isLocked && PlayerInventory.keyCount < requiredKeyCount)
        {
            promptMessage = "Door secured. Use the key to unlock.";
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
            if (PlayerInventory.keyCount >= requiredKeyCount)
            {
                UnlockDoor();
            }
            else
            {
                UpdatePromptMessage();
                return;
            }
        }

        ToggleDoor();
    }

    private void UnlockDoor()
    {
        isLocked = false;

        // Show feedback text via PlayerUI
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
