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
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Load best time from PlayerPrefs
        LoadBestTime();
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGameState();
        FindUIReferences();
    }
    
    void Start()
    {
        FindUIReferences();
        StartGame();
    }
    
    void FindUIReferences()
    {
        // Try to find UI references in the scene
        if (timerText == null)
        {
            GameObject timerGO = GameObject.Find("TimerText");
            if (timerGO != null) timerText = timerGO.GetComponent<Text>();
        }
        
        if (gameOverTimeText == null)
        {
            GameObject gameOverTimeGO = GameObject.Find("GameOverTimeText");
            if (gameOverTimeGO != null) gameOverTimeText = gameOverTimeGO.GetComponent<Text>();
        }
        
        if (bestTimeText == null)
        {
            GameObject bestTimeGO = GameObject.Find("BestTimeText");
            if (bestTimeGO != null) bestTimeText = bestTimeGO.GetComponent<Text>();
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
    public void ShowGameOver()
    {
        StopGame();
        UpdateBestTime();
        UpdateGameOverPanel();
        OnPlayerDied?.Invoke();
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
    
    void UpdateGameOverPanel()
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
        
        // You could also add coins display
        GameObject coinsTextGO = GameObject.Find("GameOverCoinsText");
        if (coinsTextGO != null)
        {
            Text coinsText = coinsTextGO.GetComponent<Text>();
            if (coinsText != null)
            {
                coinsText.text = $"Coins Collected: {currentRunCoins}";
            }
        }
    }
    
    void SaveBestTime()
    {
        PlayerPrefs.SetFloat("BestTime", longestRunTime);
        PlayerPrefs.Save();
    }
    
    void LoadBestTime()
    {
        if (PlayerPrefs.HasKey("BestTime"))
        {
            longestRunTime = PlayerPrefs.GetFloat("BestTime");
            bestTimeFormatted = FormatTime(longestRunTime);
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
        isGameRunning = true;
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
        currentRunCoins = 0;
        
        // Reset timer display
        if (timerText != null)
        {
            timerText.text = "00:00";
            timerText.color = Color.white;
        }
        
        Debug.Log("Game started - New run initialized");
        
        ResetBaseUpgrades();
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
    }
    
    // =================== RESET SYSTEM ===================
    public void ResetGameState()
    {
        Debug.Log("=== RESETTING GAME STATE ===");
        
        Time.timeScale = 1f;
        
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
        currentRunCoins = 0;
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        isGameRunning = true;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
                pc.enabled = true;
            
            PlayerAttack pa = player.GetComponent<PlayerAttack>();
            if (pa != null)
                pa.enabled = true;
        }
        
        PlayerPrefs.DeleteAll();
        
        ResetBaseUpgrades();
        
        ResetPlayerStats();
        
        ClearAllLoot();
        
        OnGameReset?.Invoke();
        
        Debug.Log("Game state reset complete - Time scale: " + Time.timeScale);
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
        
        Debug.Log($"Reset {allBases.Length} base(s)");
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
                Debug.Log($"Reset player speed to {defaultPlayerSpeed}");
            }
            
            // Reset PlayerHealth
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.maxHealth = defaultPlayerHealth;
                ph.currentHealth = defaultPlayerHealth;
                Debug.Log($"Reset player health to {defaultPlayerHealth}");
            }
            
            // Reset PlayerAttack
            PlayerAttack pa = player.GetComponent<PlayerAttack>();
            if (pa != null)
            {
                pa.attackDamage = defaultPlayerAttack;
                Debug.Log($"Reset player attack to {defaultPlayerAttack}");
            }
            
            // Reset PlayerInventory
            PlayerInventory pi = player.GetComponent<PlayerInventory>();
            if (pi != null)
            {
                pi.coins = 0;
                pi.inventory.Clear();
                Debug.Log("Reset player inventory and coins");
            }
        }
        else
        {
            Debug.LogWarning("Player not found for reset");
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
            Debug.Log($"Cleared {allLoot.Length} loot items from scene");
        }
    }
    
    // =================== TIER MANAGEMENT ===================
    void CheckTierUnlocks()
    {
        LootTier previousTier = unlockedTiers;
        
        if (gameTime >= epicTierUnlockTime && unlockedTiers < LootTier.Epic)
        {
            unlockedTiers = LootTier.Epic;
            Debug.Log("EPIC tier unlocked!");
        }
        else if (gameTime >= rareTierUnlockTime && unlockedTiers < LootTier.Rare)
        {
            unlockedTiers = LootTier.Rare;
            Debug.Log("RARE tier unlocked!");
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
        Debug.Log($"Added {amount} coins. Total this run: {currentRunCoins}");
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

    void ShowGameOver(string reason)
    {
        Debug.Log($"GameManager: Showing Game Over - {reason}");
        
        StopGame();
        UpdateBestTime();
        OnPlayerDied?.Invoke();
        
        Debug.Log("Game Over sequence complete");
    }
    
    IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetGameState();
        StartGame();
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
}

public enum LootTier
{
    Common = 0,
    Rare = 1,
    Epic = 2
}