using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Add this for scene reload

public class GameManager : MonoBehaviour
{
    [Header("Game Timer Settings")]
    public float gameTime = 0f;
    public bool isGameRunning = true;
    
    [Header("Tier Unlock Times (in seconds)")]
    public float rareTierUnlockTime = 300f;    // 5 minutes
    public float epicTierUnlockTime = 600f;    // 10 minutes
    
    [Header("Current Unlocked Tiers")]
    public LootTier unlockedTiers = LootTier.Common;
    
    [Header("Player Default Stats")]
    public float defaultPlayerSpeed = 5f;
    public int defaultPlayerHealth = 100;
    public int defaultPlayerAttack = 10;
    public int defaultBaseHealth = 500;
    
    [Header("Events")]
    public UnityEvent<LootTier> OnTierUnlocked;
    public UnityEvent<float> OnMinutePassed;
    public UnityEvent OnGameReset; // NEW: Event for when game resets
    public UnityEvent OnPlayerDied; // NEW: Event for player death
    
    private float lastMinute = 0f;
    
    // NEW: Track run stats
    private int currentRunCoins = 0;
    private int highestRunCoins = 0;
    private float longestRunTime = 0f;
    
    // Singleton pattern for easy access
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
        
        // Subscribe to scene load to reset game state
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When scene loads (including on start), reset game state
        ResetGameState();
    }
    
    void Start()
    {
        StartGame();
    }
    
    void Update()
    {
        if (isGameRunning)
        {
            gameTime += Time.deltaTime;
            CheckTierUnlocks();
            CheckMinutePassed();
        }
    }
    
    // =================== GAME STATE MANAGEMENT ===================
    public void StartGame()
    {
        isGameRunning = true;
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
        currentRunCoins = 0;
        
        Debug.Log("Game started - New run initialized");
        
        // Find and reset base upgrades
        ResetBaseUpgrades();
    }
    
    public void StopGame()
    {
        isGameRunning = false;
        
        // Update run stats
        if (gameTime > longestRunTime)
            longestRunTime = gameTime;
        
        if (currentRunCoins > highestRunCoins)
            highestRunCoins = currentRunCoins;
    }
    
    // =================== RESET SYSTEM ===================
    public void ResetGameState()
    {
        Debug.Log("=== RESETTING GAME STATE ===");
        
        // Reset timer and tiers
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
        
        // Reset PlayerPrefs to clear any saved upgrades
        PlayerPrefs.DeleteAll();
        
        // Find and reset base upgrades
        ResetBaseUpgrades();
        
        // Reset player stats
        ResetPlayerStats();
        
        // Clear all collectibles in scene (optional)
        ClearAllLoot();
        
        // Trigger reset event
        OnGameReset?.Invoke();
        
        Debug.Log("Game state reset complete");
    }
    
    void ResetBaseUpgrades()
    {
        BaseTrigger[] allBases = FindObjectsOfType<BaseTrigger>();
        foreach (BaseTrigger baseTrigger in allBases)
        {
            // Call reset method if it exists (we'll add this to BaseTrigger)
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
        // Optional: Clear all loot items in scene
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
        
        // Trigger event if tier changed
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
    
    // =================== COIN TRACKING ===================
    public void AddCoins(int amount)
    {
        currentRunCoins += amount;
        Debug.Log($"Added {amount} coins. Total this run: {currentRunCoins}");
    }
    
    public void PlayerDied()
    {
        Debug.Log("Player died! Ending run...");
        StopGame();
        OnPlayerDied?.Invoke();
        
        // Optional: Automatically reset after delay
        // StartCoroutine(ResetAfterDelay(3f));
    }
    
    IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetGameState();
        StartGame();
    }
    
    // =================== PUBLIC METHODS ===================
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    public int GetCurrentRunCoins() => currentRunCoins;
    public int GetHighestRunCoins() => highestRunCoins;
    public float GetLongestRunTime() => longestRunTime;
    public float GetCurrentRunTime() => gameTime;
    
    // Call this from PlayerHealth when player dies
    public void OnPlayerDeathEvent()
    {
        PlayerDied();
    }
}

// Extended LootType enum with tiers
public enum LootTier
{
    Common = 0,
    Rare = 1,
    Epic = 2
}