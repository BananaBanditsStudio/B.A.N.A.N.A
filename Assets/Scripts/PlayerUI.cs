using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI feedbackText; // optional separate TMP for feedback

    private Coroutine feedbackRoutine;

    private void Start()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    // Updates the normal raycast prompt (Press E to Interact)
    public void UpdateText(string promptMessage)
    {
        if (promptText == null) return;

        if (string.IsNullOrEmpty(promptMessage))
        {
            promptText.gameObject.SetActive(false);
        }
        else
        {
            promptText.text = promptMessage;
            promptText.gameObject.SetActive(true);
        }
    }

    // 🔔 NEW: Show short feedback messages like "Picked up key!"
    public void ShowFeedback(string message, float duration = 2f)
    {
        if (feedbackText == null) return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(FeedbackRoutine(message, duration));
    }

    private IEnumerator FeedbackRoutine(string message, float duration)
    {
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        feedbackText.gameObject.SetActive(false);
        feedbackRoutine = null;
    }
}
