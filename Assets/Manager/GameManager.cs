using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    
    [Header("Events")]
    public UnityEvent<LootTier> OnTierUnlocked;
    public UnityEvent<float> OnMinutePassed;
    
    private float lastMinute = 0f;
    
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
    
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    public void StartGame()
    {
        isGameRunning = true;
        gameTime = 0f;
        unlockedTiers = LootTier.Common;
    }
    
    public void StopGame()
    {
        isGameRunning = false;
    }
}

// Extended LootType enum with tiers
public enum LootTier
{
    Common = 0,
    Rare = 1,
    Epic = 2
}