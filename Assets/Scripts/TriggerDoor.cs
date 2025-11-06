using UnityEngine;

public class OfficeDoor : Interactable
{
    [Header("Door Settings")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AudioSource doorSound; // optional

    private bool isOpen = false;

    private void Start()
    {
        // optional sanity check
        if (doorAnimator == null)
            Debug.LogWarning("No animator assigned to OfficeDoor on " + gameObject.name);
    }

    protected override void Interact()
    {
        // This is called automatically by your PlayerInteract when pressing E
        ToggleDoor();
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;

        if (doorAnimator != null)
            doorAnimator.SetBool("isOpen", isOpen);

        if (doorSound != null)
            doorSound.Play();

        // Update the prompt message dynamically so PlayerInteract shows correct text next frame
        promptMessage = isOpen ? "Press E to Close" : "Press E to Open";
    }
}
