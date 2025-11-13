using UnityEngine;

public class KeyPickup : Interactable
{
    protected override void Interact()
    {
        PlayerInventory.hasKey = true;
        PlayerInventory.keyCount++;

        PlayerUI ui = FindObjectOfType<PlayerUI>();
        if (ui != null)
            ui.ShowFeedback("You picked up a key!");

        Destroy(gameObject);
    }
}
