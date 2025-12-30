using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Timer Settings")]
    public float gameTime = 0f;
    public bool isGameRunning = true;

    [Header("UI References")]
    public GameObject gameOverPanel;
    
    [Header("Tier Unlock Times (in seconds)")]
    public float rareTierUnlockTime = 300f;
    public float epicTierUnlockTime = 600f;
    
    [Header("Current Unlocked Tiers")]
    public LootTier unlockedTiers = LootTier.Common;
    
    [Header("Player Default Stats")]
    public float defaultPlayerSpeed = 5f;
    public int defaultPlayerHealth = 100;
    public int defaultPlayerAttack = 10;
    public int defaultBaseHealth = 500;
    
    [Header("UI References")]
    public Text timerText; // Assign in inspector - shows survival time
    public Text gameOverTimeText; // Assign in inspector - shows final time in GameOver panel
    public Text bestTimeText; // Assign in inspector - shows best time in GameOver panel
    
    [Header("Events")]
    public UnityEvent<LootTier> OnTierUnlocked;
    public UnityEvent<float> OnMinutePassed;
    public UnityEvent OnGameReset;
    public UnityEvent OnPlayerDied;
    
    private float lastMinute = 0f;
    
    private int currentRunCoins = 0;
    private int highestRunCoins = 0;
    private float longestRunTime = 0f;
    private string bestTimeFormatted = "00:00";
    
    public static GameManager Instance { get; private set; }
    
    private bool sceneLoadedEventSubscribed = false;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    void OnDestroy()
    {
        // Clean up scene loaded event
        if (sceneLoadedEventSubscribed)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            sceneLoadedEventSubscribed = false;
            Debug.Log("GameManager: Scene loaded event unsubscribed");
        }
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameManager: Scene loaded - {scene.name}");
        
        // Check if this is main menu
        if (scene.name == "MainMenu" || scene.buildIndex == 0)
        {
            Debug.Log("GameManager: In main menu, resetting state");
            ResetGameStateForMenu();
        }
        else
        {
            Debug.Log("GameManager: In game scene, starting new game");
            // Don't auto-reset here, let the scene setup handle it
            // Just ensure UI references are found
            FindUIReferences();
        }
    }
    
    void Start()
    {
        Debug.Log("GameManager: Start called");
        
        // Only auto-start if we're in a game scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "MainMenu" && currentScene != "Menu")
        {
            FindUIReferences();
            StartGame();
        }
    }
    
    void FindUIReferences()
    {
        Debug.Log("GameManager: Finding UI references");
        
        // Try to find UI references in the scene
        if (timerText == null)
        {
            GameObject timerGO = GameObject.Find("TimerText");
            if (timerGO != null) 
            {
                timerText = timerGO.GetComponent<Text>();
                Debug.Log("GameManager: Found TimerText");
            }
        }
        
        if (gameOverTimeText == null)
        {
            GameObject gameOverTimeGO = GameObject.Find("GameOverTimeText");
            if (gameOverTimeGO != null) 
            {
                gameOverTimeText = gameOverTimeGO.GetComponent<Text>();
                Debug.Log("GameManager: Found GameOverTimeText");
            }
        }
        
        if (bestTimeText == null)
        {
            GameObject bestTimeGO = GameObject.Find("BestTimeText");
            if (bestTimeGO != null) 
            {
                bestTimeText = bestTimeGO.GetComponent<Text>();
                Debug.Log("GameManager: Found BestTimeText");
            }
        }
        
        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.Find("GameOverPanel");
            if (gameOverPanel != null)
            {
                Debug.Log("GameManager: Found GameOverPanel");
                gameOverPanel.SetActive(false);
            }
        }
    }
    
    void Update()
    {
        if (isGameRunning)
        {
            gameTime += Time.deltaTime;
            UpdateTimerDisplay();
            CheckTierUnlocks();
            CheckMinutePassed();
        }
    }
    
    // =================== TIMER DISPLAY ===================
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = GetFormattedTime();
            
            // Optional: Color coding based on time
            if (gameTime >= epicTierUnlockTime)
                timerText.color = Color.yellow; // Epic tier
            else if (gameTime >= rareTierUnlockTime)
                timerText.color = Color.cyan; // Rare tier
            else
                timerText.color = Color.white; // Common tier
        }
    }
    
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    // =================== GAME OVER HANDLING ===================
    public void ShowGameOver(string reason = "")
    {
        Debug.Log($"GameManager: ShowGameOver called - {reason}");
        
        StopGame();
        UpdateBestTime();
        
        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            UpdateGameOverPanel(reason);
        }
        
        OnPlayerDied?.Invoke();
        
        Debug.Log("Game Over sequence complete");
    }
    
    void UpdateBestTime()
    {
        if (gameTime > longestRunTime)
        {
            longestRunTime = gameTime;
            bestTimeFormatted = GetFormattedTime();
            SaveBestTime();
        }
    }
    
    void UpdateGameOverPanel(string reason)
    {
        // Update game over panel with final stats
        if (gameOverTimeText != null)
        {
            gameOverTimeText.text = $"Survival Time: {GetFormattedTime()}";
        }
        
        if (bestTimeText != null)
        {
            bestTimeText.text = $"Best Time: {bestTimeFormatted}";
        }
        
        // Update reason if provided
        GameObject reasonTextGO = GameObject.Find("GameOverReasonText");
        if (reasonTextGO != null && !string.IsNullOrEmpty(reason))
        {
            Text reasonText = reasonTextGO.GetComponent<Text>();
            if (reasonText != null)
            {
                reasonText.text = reason;
            }
        }
    }
    
    void SaveBestTime()
    {
        PlayerPrefs.SetFloat("BestTime", longestRunTime);
        PlayerPrefs.Save();
        Debug.Log($"GameManager: Saved best time: {longestRunTime}");
    }
    
    void LoadBestTime()
    {
        if (PlayerPrefs.HasKey("BestTime"))
        {
            longestRunTime = PlayerPrefs.GetFloat("BestTime");
            bestTimeFormatted = FormatTime(longestRunTime);
            Debug.Log($"GameManager: Loaded best time: {longestRunTime}");
        }
        else
        {
            Debug.Log("GameManager: No best time saved yet");
        }
    }
    
    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    // =================== GAME STATE MANAGEMENT ===================
    public void StartGame()
    {
        Debug.Log("GameManager: StartGame called");
        
        isGameRunning = true;
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
        currentRunCoins = 0;
        lastMinute = 0f;
        
        // Reset timer display
        if (timerText != null)
        {
            timerText.text = "00:00";
            timerText.color = Color.white;
        }
        
        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        Debug.Log("GameManager: Game started - New run initialized");
    }
    
    public void StopGame()
    {
        isGameRunning = false;
        
        if (gameTime > longestRunTime)
        {
            longestRunTime = gameTime;
            SaveBestTime();
        }
        
        if (currentRunCoins > highestRunCoins)
            highestRunCoins = currentRunCoins;
            
        Debug.Log("GameManager: Game stopped");
    }
    
    // =================== RESET SYSTEM ===================
    public void ResetGameState()
    {
        Debug.Log("GameManager: === RESETTING GAME STATE ===");
        
        Time.timeScale = 1f;
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
        currentRunCoins = 0;
        lastMinute = 0f;
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        isGameRunning = true;
        
        // Find and enable player components
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
                pc.enabled = true;
            
            PlayerAttack pa = player.GetComponent<PlayerAttack>();
            if (pa != null)
                pa.enabled = true;
                
            Debug.Log("GameManager: Player components enabled");
        }
        
        // Don't delete all PlayerPrefs here - only run-specific data
        PlayerPrefs.DeleteKey("CurrentWave");
        PlayerPrefs.DeleteKey("CurrentScore");
        
        ResetBaseUpgrades();
        ResetPlayerStats();
        ClearAllLoot();
        
        OnGameReset?.Invoke();
        
        Debug.Log("GameManager: Game state reset complete");
    }
    
    void ResetGameStateForMenu()
    {
        Debug.Log("GameManager: Resetting for main menu");
        
        // Reset basic state but don't touch scene objects
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
        currentRunCoins = 0;
        isGameRunning = false;
        Time.timeScale = 1f;
        
        // Hide game over panel if it exists
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    void ResetBaseUpgrades()
    {
        BaseTrigger[] allBases = FindObjectsOfType<BaseTrigger>();
        foreach (BaseTrigger baseTrigger in allBases)
        {
            System.Reflection.MethodInfo resetMethod = 
                typeof(BaseTrigger).GetMethod("ResetForNewRun");
            
            if (resetMethod != null)
            {
                resetMethod.Invoke(baseTrigger, null);
            }
        }
        
        Debug.Log($"GameManager: Reset {allBases.Length} base(s)");
    }
    
    void ResetPlayerStats()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Reset PlayerController
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.moveSpeed = defaultPlayerSpeed;
                Debug.Log($"GameManager: Reset player speed to {defaultPlayerSpeed}");
            }
            
            // Reset PlayerHealth
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.maxHealth = defaultPlayerHealth;
                ph.currentHealth = defaultPlayerHealth;
                Debug.Log($"GameManager: Reset player health to {defaultPlayerHealth}");
            }
            
            // Reset PlayerAttack
            PlayerAttack pa = player.GetComponent<PlayerAttack>();
            if (pa != null)
            {
                pa.attackDamage = defaultPlayerAttack;
                Debug.Log($"GameManager: Reset player attack to {defaultPlayerAttack}");
            }
            
            // Reset PlayerInventory
            PlayerInventory pi = player.GetComponent<PlayerInventory>();
            if (pi != null)
            {
                pi.coins = 0;
                pi.inventory.Clear();
                Debug.Log("GameManager: Reset player inventory and coins");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: Player not found for reset");
        }
    }
    
    void ClearAllLoot()
    {
        CollectibleItem[] allLoot = FindObjectsOfType<CollectibleItem>();
        foreach (CollectibleItem loot in allLoot)
        {
            Destroy(loot.gameObject);
        }
        
        if (allLoot.Length > 0)
        {
            Debug.Log($"GameManager: Cleared {allLoot.Length} loot items from scene");
        }
    }
    
    // =================== TIER MANAGEMENT ===================
    void CheckTierUnlocks()
    {
        LootTier previousTier = unlockedTiers;
        
        if (gameTime >= epicTierUnlockTime && unlockedTiers < LootTier.Epic)
        {
            unlockedTiers = LootTier.Epic;
            Debug.Log("GameManager: EPIC tier unlocked!");
        }
        else if (gameTime >= rareTierUnlockTime && unlockedTiers < LootTier.Rare)
        {
            unlockedTiers = LootTier.Rare;
            Debug.Log("GameManager: RARE tier unlocked!");
        }
        
        if (unlockedTiers != previousTier)
        {
            OnTierUnlocked?.Invoke(unlockedTiers);
        }
    }
    
    void CheckMinutePassed()
    {
        float currentMinute = Mathf.Floor(gameTime / 60f);
        if (currentMinute > lastMinute)
        {
            lastMinute = currentMinute;
            OnMinutePassed?.Invoke(currentMinute);
        }
    }
    
    public bool IsTierUnlocked(LootTier tier)
    {
        return unlockedTiers >= tier;
    }
    
    public void AddCoins(int amount)
    {
        currentRunCoins += amount;
        Debug.Log($"GameManager: Added {amount} coins. Total this run: {currentRunCoins}");
    }
    
    public void PlayerDied()
    {
        Debug.Log("GameManager: PlayerDied called");
        ShowGameOver("YOU DIED");
    }

    public void OnBaseDestroyed()
    {
        Debug.Log("GameManager: Base destroyed!");
        ShowGameOver("BASE DESTROYED");
    }
    
    // =================== PUBLIC METHODS ===================
    public int GetCurrentRunCoins() => currentRunCoins;
    public int GetHighestRunCoins() => highestRunCoins;
    public float GetLongestRunTime() => longestRunTime;
    public float GetCurrentRunTime() => gameTime;
    
    public void OnPlayerDeathEvent()
    {
        PlayerDied();
    }
    
    // Clean reset for scene reload
    public void CleanResetForNewScene()
    {
        Debug.Log("GameManager: Clean reset for new scene");
        ResetGameState();
        StartGame();
    }
}

public enum LootTier
{
    Common = 0,
    Rare = 1,
    Epic = 2
}