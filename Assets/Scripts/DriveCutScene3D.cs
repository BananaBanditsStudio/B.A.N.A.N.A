using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class DriveCutscene3D : MonoBehaviour
{
    [Header("References")]
    public Transform car;
    public Transform stopPoint;
    public ParallaxLayer3D[] layers;
    [Header("UI")]
    public Image fadeImage;

    [Header("Motion")]
    public float driveSpeed = 10f;      // start speed
    public float slowDistance = 12f;    // start easing this far away
    public float minSpeed = 1.5f;       // floor while easing
    public float afterStopDelay = 0.8f; // pause before fade
    public float fadeDuration = 1.0f;

    [Header("Next Scene")]
    public string nextScene = "Level1_GasStation";

    bool reached = false;
    bool fading = false;
    
    public AudioSource engineAudio;
    public float maxPitch = 1.0f;
    public float minPitch = 0.7f;

    void Update()
    {
        if (reached) return;

        float d = Vector3.Distance(car.position, stopPoint.position);
        float curSpeed = driveSpeed;

        if (d < slowDistance)
            curSpeed = Mathf.Lerp(minSpeed, driveSpeed, d / slowDistance); // ease down

        if (engineAudio != null) {
            float t = Mathf.InverseLerp(minSpeed, driveSpeed, curSpeed);
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, t);
        }

        // Move the car along +X
        Vector3 nextPos = Vector3.MoveTowards(car.position, stopPoint.position, curSpeed * Time.deltaTime);
        float movedX = nextPos.x - car.position.x;
        car.position = nextPos;

        // Scroll parallax layers opposite to car motion
        float worldUnits = Mathf.Abs(movedX);
        foreach (var l in layers) l.Scroll(worldUnits);

        // Arrived?
        if (Vector3.Distance(car.position, stopPoint.position) < 0.05f)
        {
            reached = true;
            Invoke(nameof(BeginFade), afterStopDelay);
        }

        // Skip cutscene on any key
        if (Input.anyKeyDown && !fading)
        {
            reached = true;
            BeginFade();
        }
    }

    void BeginFade()
    {
        if (!fading) StartCoroutine(FadeThenLoad());
    }

    IEnumerator FadeThenLoad()
    {
        fading = true;
        Color c = fadeImage.color;
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        SceneManager.LoadScene(nextScene);
    }
}
