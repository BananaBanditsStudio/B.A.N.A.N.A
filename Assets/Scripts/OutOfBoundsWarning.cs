using UnityEngine;

public class OutOfBoundsWarning : MonoBehaviour
{
    public string warningMessage = "You cannot go this way!";
    public float displayDuration = 2f;
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerUI ui = FindObjectOfType<PlayerUI>();
            if (ui != null)
            {
                ui.ShowFeedback(warningMessage, displayDuration);
            }
        }
    }
}

