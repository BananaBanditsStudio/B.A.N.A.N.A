using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LogoIntroFull : MonoBehaviour
{
    [Header("UI Element")]
    public Image logo;

    [Header("Timing (seconds)")]
    public float fadeInTime = 1.5f;
    public float holdTime = 1.5f;
    public float fadeOutTime = 1.5f;

    void Start()
    {
        StartCoroutine(PlayLogoSequence());
    }

    IEnumerator PlayLogoSequence()
    {
        Color c = logo.color;

        // -------------------------
        // Fade In
        // -------------------------
        float t = 0;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeInTime);
            logo.color = c;
            yield return null;
        }

        // -------------------------
        // Hold
        // -------------------------
        yield return new WaitForSeconds(holdTime);

        // -------------------------
        // Fade Out
        // -------------------------
        t = 0;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1, 0, t / fadeOutTime);
            logo.color = c;
            yield return null;
        }

        // Disable logo when done
        gameObject.SetActive(false);
    }
}
