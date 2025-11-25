using UnityEngine;

public class KeyCardPickup : Interactable
{
    [Header("Pickup Settings")]
    [SerializeField] private string feedbackMessage = "You collected the vault secrets!";
    [SerializeField] private AudioClip pickupSound;

    void Start()
    {
        promptMessage = "Press E to collect vault secrets";
    }

    protected override void Interact()
    {
        GameObjectiveManager.CollectKeyCard();

        PlayerUI ui = FindObjectOfType<PlayerUI>();
        if (ui != null)
            ui.ShowFeedback(feedbackMessage);

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        Destroy(gameObject);
    }
}

