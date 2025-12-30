using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoCutsceneManager : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public VideoClip introCutscene;
    public bool playOnStart = true;
    public bool loopVideo = false;
    
    [Header("UI Elements")]
    public GameObject cutsceneCanvas;
    public RawImage videoDisplay;
    public GameObject skipButton;
    public Text skipText;
    public Text timerText;
    
    [Header("Skip Settings")]
    public KeyCode skipKey = KeyCode.Space;
    public KeyCode skipKeyAlt = KeyCode.Escape;
    public float skipDelay = 2f; // Seconds before skip is available
    public string sceneToLoadAfter = "MainGame"; // Your game scene name
    
    [Header("Fade Settings")]
    public Image fadeOverlay;
    public float fadeDuration = 1f;
    
    private bool isPlaying = false;
    private bool canSkip = false;
    private float videoTimer = 0f;
    private Coroutine skipDelayCoroutine;
    
    void Awake()
    {
        // Ensure only one instance exists
        if (FindObjectsOfType<VideoCutsceneManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeVideoPlayer();
        
        if (playOnStart && introCutscene != null)
        {
            PlayCutscene();
        }
        else
        {
            // Skip directly to game
            LoadGameScene();
        }
    }
    
    void InitializeVideoPlayer()
    {
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = loopVideo;
            videoPlayer.skipOnDrop = true;
            videoPlayer.targetCameraAlpha = 0f;
        }
        
        // Setup video rendering
        if (videoDisplay != null)
        {
            videoPlayer.targetTexture = new RenderTexture((int)videoDisplay.rectTransform.rect.width, 
                                                         (int)videoDisplay.rectTransform.rect.height, 24);
            videoDisplay.texture = videoPlayer.targetTexture;
        }
    }
    
    void Update()
    {
        if (!isPlaying) return;
        
        videoTimer += Time.deltaTime;
        
        // Update timer display
        if (timerText != null)
        {
            float remaining = (float)(videoPlayer.length - videoPlayer.time);
            timerText.text = FormatTime(remaining);
        }
        
        // Skip input
        if (canSkip && (Input.GetKeyDown(skipKey) || Input.GetKeyDown(skipKeyAlt) || Input.GetMouseButtonDown(0)))
        {
            SkipCutscene();
        }
        
        // Auto-advance when video ends
        if (!loopVideo && videoPlayer.time >= videoPlayer.length - 0.1f)
        {
            OnVideoEnd();
        }
    }
    
    public void PlayCutscene()
    {
        if (introCutscene == null)
        {
            Debug.LogError("No intro cutscene assigned!");
            LoadGameScene();
            return;
        }
        
        isPlaying = true;
        canSkip = false;
        videoTimer = 0f;
        
        // Show UI
        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(true);
        if (skipButton != null) skipButton.SetActive(false);
        if (skipText != null) skipText.gameObject.SetActive(false);
        
        // Setup video
        videoPlayer.clip = introCutscene;
        videoPlayer.Play();
        
        // Start skip delay
        if (skipDelayCoroutine != null) StopCoroutine(skipDelayCoroutine);
        skipDelayCoroutine = StartCoroutine(SkipDelayRoutine());
        
        Debug.Log("Playing intro cutscene...");
    }
    
    IEnumerator SkipDelayRoutine()
    {
        yield return new WaitForSeconds(skipDelay);
        
        canSkip = true;
        
        // Show skip UI
        if (skipButton != null) skipButton.SetActive(true);
        if (skipText != null)
        {
            skipText.gameObject.SetActive(true);
            skipText.text = $"Press {skipKey} or {skipKeyAlt} to Skip";
        }
        
        Debug.Log("Skip now available");
    }
    
    public void SkipCutscene()
    {
        if (!isPlaying) return;
        
        Debug.Log("Cutscene skipped by player");
        
        // Stop video
        videoPlayer.Stop();
        isPlaying = false;
        
        // Start fade out
        StartCoroutine(FadeOutAndLoadScene());
    }
    
    void OnVideoEnd()
    {
        if (!isPlaying) return;
        
        Debug.Log("Cutscene finished playing");
        isPlaying = false;
        
        StartCoroutine(FadeOutAndLoadScene());
    }
    
    IEnumerator FadeOutAndLoadScene()
    {
        // Hide UI elements
        if (skipButton != null) skipButton.SetActive(false);
        if (skipText != null) skipText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        
        // Fade to black
        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            Color color = fadeOverlay.color;
            color.a = 0f;
            fadeOverlay.color = color;
            fadeOverlay.gameObject.SetActive(true);
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = color;
                yield return null;
            }
        }
        
        // Hide canvas
        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(false);
        
        // Load game scene
        LoadGameScene();
    }
    
    void LoadGameScene()
    {
        Debug.Log($"Loading game scene: {sceneToLoadAfter}");
        
        // If we're already in the game scene, just disable ourselves
        if (SceneManager.GetActiveScene().name == sceneToLoadAfter)
        {
            gameObject.SetActive(false);
            return;
        }
        
        SceneManager.LoadScene(sceneToLoadAfter);
        
        // Destroy after scene load (scene will have its own VideoCutsceneManager if needed)
        Destroy(gameObject);
    }
    
    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    // For button click
    public void OnSkipButtonClicked()
    {
        SkipCutscene();
    }
    
    // Public method to play specific cutscene
    public void PlayCustomCutscene(VideoClip clip)
    {
        introCutscene = clip;
        PlayCutscene();
    }
}