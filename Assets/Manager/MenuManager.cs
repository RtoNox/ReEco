using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuPanel;
    
    [Header("Pause Menu References")]
    public Button continueButton;
    public Button retryButton;
    public Button mainMenuButton;
    
    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    
    private bool isPaused = false;
    private bool canPause = true;
    
    // No singleton, no DontDestroyOnLoad
    // Each scene will have its own MenuManager
    
    void Start()
    {
        Debug.Log("MenuManager: Start");
        
        InitializeButtons();
        
        // Hide pause menu on start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Always show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Check scene
        CheckCurrentScene();
        
        // Ensure time is running
        Time.timeScale = 1f;
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"MenuManager: Scene loaded - {scene.name}");
        
        // Reset everything when scene loads
        ResetOnSceneLoad();
    }
    
    void ResetOnSceneLoad()
    {
        Debug.Log("MenuManager: Resetting on scene load");
        
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        CheckCurrentScene();
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Re-enable player input
        EnablePlayerInput(true);
    }
    
    void CheckCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        canPause = !(sceneName == "MainMenu" || sceneName == "Menu");
        Debug.Log($"MenuManager: Scene '{sceneName}' - Can pause: {canPause}");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(pauseKey) && canPause && IsGameRunning())
        {
            TogglePause();
        }
    }
    
    bool IsGameRunning()
    {
        return GameManager.Instance != null && GameManager.Instance.isGameRunning;
    }
    
    void InitializeButtons()
    {
        Debug.Log("MenuManager: Initializing buttons");
        
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueGame);
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }
    
    public void TogglePause()
    {
        if (!canPause) return;
        
        isPaused = !isPaused;
        
        if (isPaused)
            PauseGame();
        else
            ContinueGame();
    }
    
    public void PauseGame()
    {
        Debug.Log("MenuManager: Pausing game");
        
        isPaused = true;
        Time.timeScale = 0f;
        
        // Disable player input
        EnablePlayerInput(false);
        
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void ContinueGame()
    {
        Debug.Log("MenuManager: Continuing game");
        
        isPaused = false;
        Time.timeScale = 1f;
        
        // Re-enable player input
        EnablePlayerInput(true);
        
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void OnRetryGame()
    {
        Debug.Log("MenuManager: Retry button clicked");
        
        // Immediately reset everything
        isPaused = false;
        Time.timeScale = 1f;
        
        // Hide pause menu immediately
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Reset GameManager if it exists
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
        }
        
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // RELOAD THE CURRENT SCENE
        // This is the key - clean reload
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        // Note: This MenuManager will be destroyed and a new one created
    }
    
    public void GoToMainMenu()
    {
        Debug.Log("MenuManager: Main Menu button clicked");
        
        // Immediately reset everything
        isPaused = false;
        Time.timeScale = 1f;
        
        // Hide pause menu immediately
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Load main menu scene (scene 0)
        SceneManager.LoadScene(0);
        
        // Note: This MenuManager will be destroyed
    }
    
    void EnablePlayerInput(bool enable)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.enabled = enable;
            Debug.Log($"MenuManager: Player input {(enable ? "enabled" : "disabled")}");
        }
    }
}