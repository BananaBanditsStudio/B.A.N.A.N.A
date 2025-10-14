using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        public Texture image;
        [TextArea(3, 5)] public string text;
        public AudioClip narration;
    }

    [Header("UI References")]
    public RawImage slideImage;
    public TMP_Text slideText;
    public Button skipButton;
    public CanvasGroup fadeGroup;
    public AudioSource audioSource;
    public Image blackBackground;

    [Header("Slide Data")]
    public Slide[] slides;

    [Header("Transition Settings")]
    public float fadeDuration = 0.8f;
    public float blackScreenDuration = 0.3f;
    public float typeSpeed = 0.03f;
    public float audioFadeDuration = 0.5f;

    private int currentIndex = 0;
    private bool isFading = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine audioFadeCoroutine;

    void Start()
    {
        skipButton.onClick.AddListener(SkipCutscene);
        fadeGroup.alpha = 1f;
        
        // Ensure black background is hidden initially
        if (blackBackground != null)
        {
            blackBackground.gameObject.SetActive(false);
        }

        ShowSlide(currentIndex, true);
        PlayNarration(currentIndex);
        typingCoroutine = StartCoroutine(TypeText(slides[currentIndex].text));
    }

    void Update()
    {
        if (isFading) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // Click during typing: instantly finish text but KEEP narration going
                FinishTypingInstantly();
            }
            else
            {
                // Click after text finished: advance slide (fade text + audio)
                StartCoroutine(NextSlide());
            }
        }
    }

    void ShowSlide(int index, bool instant = false)
    {
        if (index >= 0 && index < slides.Length)
        {
            slideImage.texture = slides[index].image;
            slideText.text = "";
        }

        if (instant)
            fadeGroup.alpha = 1f;
    }

    IEnumerator NextSlide()
    {
        isFading = true;

        // Stop typing (if active) and fade out narration
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        yield return StartCoroutine(FadeOutNarration());

        // Show black background and fade out to black
        if (blackBackground != null)
        {
            blackBackground.gameObject.SetActive(true);
        }
        yield return StartCoroutine(Fade(1f, 0f));

        currentIndex++;
        if (currentIndex >= slides.Length)
        {
            EndCutscene();
            yield break;
        }

        // Show and fade in next slide
        ShowSlide(currentIndex, false);
        yield return StartCoroutine(Fade(0f, 1f));

        // Hide black background after fade in
        if (blackBackground != null)
        {
            blackBackground.gameObject.SetActive(false);
        }

        // Start new narration + typewriter
        PlayNarration(currentIndex);
        typingCoroutine = StartCoroutine(TypeText(slides[currentIndex].text));

        isFading = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            fadeGroup.alpha = Mathf.Lerp(from, to, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        fadeGroup.alpha = to;
    }

    IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        slideText.text = "";
        foreach (char c in fullText)
        {
            slideText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }

    void FinishTypingInstantly()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        slideText.text = slides[currentIndex].text;
        isTyping = false;
        // 🎧 narration keeps playing — no fade or stop here!
    }

    void PlayNarration(int index)
    {
        if (slides[index].narration != null)
        {
            if (audioFadeCoroutine != null)
                StopCoroutine(audioFadeCoroutine);

            audioSource.Stop();
            audioSource.volume = 1f;
            audioSource.clip = slides[index].narration;
            audioSource.Play();
        }
    }

    IEnumerator FadeOutNarration()
    {
        if (audioSource.isPlaying)
        {
            if (audioFadeCoroutine != null)
                StopCoroutine(audioFadeCoroutine);
            audioFadeCoroutine = StartCoroutine(AudioFadeCoroutine(audioSource, 0f, audioFadeDuration));
            yield return audioFadeCoroutine;
        }
    }

    IEnumerator AudioFadeCoroutine(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float time = 0f;

        while (time < duration)
        {
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        source.volume = targetVolume;
        if (targetVolume == 0f)
            source.Stop();
    }

    void SkipCutscene()
    {
        StartCoroutine(FadeOutNarration());
        EndCutscene();
    }

    void EndCutscene()
    {
        SceneManager.LoadScene("TitleScreen"); // replace with your next scene
    }
}
