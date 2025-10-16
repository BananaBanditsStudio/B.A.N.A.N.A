using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class musicmenu : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer; // Your Audio Mixer
    [SerializeField] private Slider musicSlider;    // Music volume slider
    [SerializeField] private Slider sfxSlider;      // SFX volume slider

    private void Start()
    {
        // Load saved volumes or defaults
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            SetMusicVolume(musicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
            SetSFXVolume(sfxVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1)) * 20;
        audioMixer.SetFloat("MusicVolume", dB);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1)) * 20;
        audioMixer.SetFloat("SFXVolume", dB);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }
}
