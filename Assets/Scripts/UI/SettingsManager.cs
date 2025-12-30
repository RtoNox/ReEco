using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public AudioMixer audioMixer;

    // VOLUME
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("Volume", volume);
    }

    // FULLSCREEN
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    // BACK TO MENU
    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void Start()
    {
        // Load saved settings
        if (PlayerPrefs.HasKey("Volume"))
        {
            float volume = PlayerPrefs.GetFloat("Volume");
            audioMixer.SetFloat("MasterVolume", volume);
        }

        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen") == 1;
        }
    }
}
