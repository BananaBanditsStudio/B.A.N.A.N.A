using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SplashScreenController : MonoBehaviour
{
    public Image blackPanel;
    public Image logo;

    public float logoFadeInTime = 1.2f;
    public float logoHoldTime = 1.2f;
    public float fadeOutTime = 1.2f;

    // 🔑 This remembers if the splash has already played
    private static bool hasPlayed = false;

    private void Awake()
    {
        // If we've already shown the splash once this run, skip it
        if (hasPlayed)
        {
            gameObject.SetActive(false);   // Hide SplashCanvas instantly
            return;
        }

        hasPlayed = true; // First time: mark as played
    }

    private void Start()
    {
        // Only runs on the first time (because otherwise we early-return in Awake)
        StartCoroutine(RunSplash());
    }

    IEnumerator RunSplash()
    {
        Color black = blackPanel.color;
        Color logoCol = logo.color;

        // Start: black screen, logo invisible
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

        // --- Fade both logo + black out ---
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

        // Remove splash, menu shows
        gameObject.SetActive(false);
    }
}
