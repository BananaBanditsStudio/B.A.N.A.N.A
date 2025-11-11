using UnityEngine;
using System.Collections;

public class EnvironmentSound : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource cricketAudio; // Assign in Inspector

    private int buildingCount = 0; // Tracks nested building triggers

    void Start()
    {
        if (cricketAudio != null)
        {
            cricketAudio.loop = true;
            cricketAudio.Play();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Building"))
        {
            buildingCount++;
            if (cricketAudio != null && cricketAudio.isPlaying)
            {
                cricketAudio.Stop(); // Instantly stop sound
                // Optional: StartCoroutine(FadeOut(cricketAudio, 1f));
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Building"))
        {
            buildingCount--;
            if (buildingCount <= 0 && cricketAudio != null && !cricketAudio.isPlaying)
            {
                cricketAudio.Play(); // Instantly resume sound
                // Optional: StartCoroutine(FadeIn(cricketAudio, 1f));
            }
        }
    }

    // Optional: Smooth fade out/in effect
    IEnumerator FadeOut(AudioSource audio, float duration)
    {
        float startVolume = audio.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            audio.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }
        audio.Stop();
        audio.volume = startVolume;
    }

    IEnumerator FadeIn(AudioSource audio, float duration)
    {
        float targetVolume = 1f;
        audio.volume = 0f;
        audio.Play();
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            audio.volume = Mathf.Lerp(0f, targetVolume, t / duration);
            yield return null;
        }
        audio.volume = targetVolume;
    }
}
