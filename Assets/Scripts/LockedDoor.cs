using UnityEngine;

public class LockedDoor : Interactable
{
    [Header("Door Settings")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AudioSource doorSound; // optional
    [SerializeField] private bool startsLocked = true; // toggle in Inspector

    private bool isLocked;
    private bool isOpen = false;

    private void Start()
    {
        isLocked = startsLocked;

        // Initialize prompt message at start
        if (isLocked)
            promptMessage = "Door secured. Use the key to unlock.";
        else
            promptMessage = "Press E to Open";
    }

    private void Update()
    {
        // Continuously refresh the prompt if the door is locked
        // so it updates as soon as you pick up the key
        if (isLocked && PlayerInventory.keyCount >= 2)
        {
            promptMessage = "Access granted. Press E to unlock.";
        }
        else if (isLocked && PlayerInventory.keyCount < 2)
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
            if (PlayerInventory.keyCount >= 2)
            {
                UnlockDoor();
            }
            else
            {
                promptMessage = "Door secured. Use the key to unlock.";
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

        promptMessage = "Press E to Open";
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;

        if (doorAnimator != null)
            doorAnimator.SetBool("isOpen", isOpen);

        if (doorSound != null)
            doorSound.Play();

        promptMessage = isOpen ? "Press E to Close" : "Press E to Open";
    }
}
