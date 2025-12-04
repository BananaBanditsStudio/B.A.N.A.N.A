using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SplashScreenController : MonoBehaviour
{
    public Image blackPanel;
    public Image logo;

    public float logoFadeInTime = 1.2f;
    public float logoHoldTime   = 1.2f;
    public float fadeOutTime    = 1.2f;

    void Start()
    {
        StartCoroutine(RunSplash());
    }

    IEnumerator RunSplash()
    {
        // Start: black screen, logo invisible
        Color black = blackPanel.color;
        Color logoCol = logo.color;
        black.a = 1f;
        logoCol.a = 0f;
        blackPanel.color = black;
        logo.color = logoCol;

        // --- Fade logo in over black ---
        float t = 0f;
        while (t < logoFadeInTime)
        {
            t += Time.deltaTime;
            logoCol.a = Mathf.Lerp(0f, 1f, t / logoFadeInTime);
            logo.color = logoCol;
            yield return null;
        }

        // --- Hold ---
        yield return new WaitForSeconds(logoHoldTime);

        // --- Fade both logo + black panel out together ---
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            black.a = a;
            logoCol.a = a;
            blackPanel.color = black;
            logo.color = logoCol;
            yield return null;
        }

        // Remove splash, menu is now visible
        gameObject.SetActive(false);
    }
}
