using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public AudioMixer audioMixer;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        audioMixer.SetFloat("MasterVolume", 0f);
        audioMixer.SetFloat("MusicVolume", 0f);
        audioMixer.SetFloat("SFXVolume", 0f);

        masterSlider.SetValueWithoutNotify(1f);
        musicSlider.SetValueWithoutNotify(1f);
        sfxSlider.SetValueWithoutNotify(1f);

        // Fullscreen restore
        if (PlayerPrefs.HasKey("Fullscreen"))
            Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen") == 1;
    }

    public void SetMasterVolume(float v)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Lerp(-20f, 0f, v));
    }

    public void SetMusicVolume(float v)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Lerp(-20f, 0f, v));
    }

    public void SetSFXVolume(float v)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Lerp(-20f, 0f, v));
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
