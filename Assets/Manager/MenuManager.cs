using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuPanel;
    
    [Header("Pause Menu References")]
    public Text pauseMenuTitle;
    public Button continueButton;
    public Button retryButton;
    public Button mainMenuButton;
    
    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool canPause = true;
    
    // Main menu scene index
    public int mainMenuSceneIndex = 0;
    
    [Header("Cursor Settings")]
    public bool alwaysShowCursor = true; // Set to true to always show cursor
    
    private bool isPaused = false;
    
    // Singleton pattern
    public static MenuManager Instance { get; private set; }
    
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
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name} (Index: {scene.buildIndex})");
        
        // Check if this is the main menu scene
        bool isMainMenuScene = scene.buildIndex == mainMenuSceneIndex;
        
        if (isMainMenuScene)
        {
            // Don't allow pause in main menu
            canPause = false;
            
            // Hide pause menu
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            // Ensure time is running
            Time.timeScale = 1f;
            
            // ALWAYS show cursor in main menu
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Destroy MenuManager in main menu
            Destroy(gameObject);
        }
        else
        {
            // Game scene loaded
            canPause = true;
            isPaused = false;
            
            // CRITICAL: Ensure time is running when scene loads
            Time.timeScale = 1f;
            
            // Hide pause menu
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            // Enable player input
            EnablePlayerInput(true);
            
            // CURSOR SETTINGS - Always visible during gameplay
            if (alwaysShowCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                // Traditional FPS style: hide cursor during gameplay
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
    
    void Start()
    {
        InitializeButtons();
        
        // Hide pause menu on start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Initialize cursor on start
        if (alwaysShowCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(pauseKey) && canPause && !IsGameOver())
        {
            TogglePause();
        }
    }
    
    bool IsGameOver()
    {
        // Check if game is over through GameManager
        if (GameManager.Instance == null) return false;
        return !GameManager.Instance.isGameRunning;
    }
    
    void InitializeButtons()
    {
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
    
    // =================== PAUSE MENU ===================
    public void TogglePause()
    {
        if (IsGameOver()) return; // Can't pause when game over
        
        isPaused = !isPaused;
        
        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ContinueGame();
        }
    }
    
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        // Disable player input
        EnablePlayerInput(false);
        
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            if (pauseMenuTitle != null)
                pauseMenuTitle.text = "GAME PAUSED";
        }
        
        // ALWAYS show cursor in pause menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("Game Paused");
    }
    
    public void ContinueGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        // Re-enable player input
        EnablePlayerInput(true);
        
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Cursor behavior when returning to gameplay
        if (alwaysShowCursor)
        {
            // Keep cursor visible
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // Hide cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        Debug.Log("Game Continued");
    }
    
    // =================== BUTTON ACTIONS ===================
    public void OnRetryGame()
    {
        Debug.Log("Retry from pause menu");
        
        // Reset pause state
        isPaused = false;
        
        // CRITICAL: Unfreeze time
        Time.timeScale = 1f;
        
        // Hide pause menu immediately
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Reset game state through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
            GameManager.Instance.StartGame();
        }
        
        // Enable player input
        EnablePlayerInput(true);
        
        // Cursor on retry
        if (alwaysShowCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        Debug.Log("Game Retry");
    }
    
    public void GoToMainMenu()
    {
        Debug.Log("Going to main menu from pause");
        
        // Reset pause state
        isPaused = false;
        Time.timeScale = 1f;
        
        // Hide pause menu
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Reset GameManager for fresh start
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
        }
        
        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneIndex);
        
        Debug.Log("Going to Main Menu");
        
        // MenuManager will be destroyed by OnSceneLoaded when main menu loads
    }
    
    // =================== UTILITY METHODS ===================
    void EnablePlayerInput(bool enable)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.enabled = enable;
        }
    }
    
    public bool IsGamePaused()
    {
        return isPaused;
    }
    
    // Public method to manually control cursor
    public void SetCursorVisibility(bool visible, bool locked = false)
    {
        Cursor.visible = visible;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
}