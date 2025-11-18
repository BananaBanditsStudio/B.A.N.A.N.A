using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeTransition : MonoBehaviour
{
    public CanvasGroup fadeCanvas;
    public AudioSource carAudio;
    public AudioClip carSound;
    public float fadeDuration = 0.8f; // Fade to black duration (total transition will be ~1 second)
    public string nextSceneName = "TitleScreen";

    void Awake()
    {
        if (fadeCanvas == null)
            fadeCanvas = GetComponent<CanvasGroup>();

        fadeCanvas.alpha = 0; // start clear
    }

    public void StartFadeOut()
    {
        StartCoroutine(FadeAndLoad());
    }

    private System.Collections.IEnumerator FadeAndLoad()
    {
        // Optional: play car sound
        if (carAudio != null && carSound != null)
            carAudio.PlayOneShot(carSound);

        // Fade to black
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = 1f;

        yield return new WaitForSeconds(0.2f); // brief pause on black (total transition ~1 second)

        SceneManager.LoadScene(nextSceneName);
    }
}
