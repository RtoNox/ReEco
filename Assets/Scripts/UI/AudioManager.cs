using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer")]
    public AudioMixer audioMixer;

    [Header("Music")]
    public Sound[] music;

    [Header("SFX")]
    public Sound[] sfx;

    Sound currentMusic;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupSounds(music, "Music");
        SetupSounds(sfx, "SFX");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void SetupSounds(Sound[] sounds, string mixerGroupName)
    {
        AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(mixerGroupName);

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.playOnAwake = false;

            // 🔴 THIS LINE IS THE KEY
            s.source.outputAudioMixerGroup = groups[0];
        }
    }

    public void PlayMusic(string name)
    {
        Sound s = System.Array.Find(music, m => m.name == name);
        if (s == null) return;

        if (currentMusic == s && s.source.isPlaying) return;

        if (currentMusic != null)
            currentMusic.source.Stop();

        currentMusic = s;
        s.source.Play();
    }

    public void PlaySFX(string name)
    {
        Sound s = System.Array.Find(sfx, sfx => sfx.name == name);
        if (s == null) return;

        s.source.PlayOneShot(s.clip);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || scene.name == "Settings")
            PlayMusic("MainMenu");
        else
            PlayMusic("Battle");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
