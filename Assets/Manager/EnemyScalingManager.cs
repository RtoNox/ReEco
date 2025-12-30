using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScalingManager : MonoBehaviour
{
    public static EnemyScalingManager Instance { get; private set; }
    
    [Header("Time Tracking")]
    public float gameTime = 0f;
    public float minutesPassed = 0f;
    
    [Header("Speed Scaling - PER MINUTE")]
    public float speedIncreasePerMinute = 0.05f; // 5% per minute
    public float currentSpeedMultiplier = 1f;
    
    [Header("HP Scaling - EVERY 5 MINUTES")]
    public float hpIncreasePer5Minutes = 0.25f; // 25% every 5 minutes
    public float currentHPMultiplier = 1f;
    
    [Header("Attack Scaling - EVERY 5 MINUTES")]
    public float attackIncreasePer5Minutes = 0.20f; // 20% every 5 minutes
    public float currentAttackMultiplier = 1f;
    
    [Header("Player Reference")]
    public PlayerController playerController;
    private float playerBaseSpeed;
    private float playerCurrentMaxSpeed;
    
    [Header("Debug Info")]
    public float lastHPScaleTime = 0f;
    public float lastAttackScaleTime = 0f;
    public int hpScaleCount = 0;
    public int attackScaleCount = 0;
    
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
    
    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        
        if (playerController != null)
        {
            playerBaseSpeed = playerController.moveSpeed;
            playerCurrentMaxSpeed = playerBaseSpeed;
        }
        
        StartCoroutine(ScalingUpdateRoutine());
    }
    
    void Update()
    {
        gameTime += Time.deltaTime;
        minutesPassed = gameTime / 60f;
        
        // Update player's current max speed (if player gets speed upgrades)
        if (playerController != null)
        {
            playerCurrentMaxSpeed = playerController.moveSpeed;
        }
    }
    
    IEnumerator ScalingUpdateRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Update every second
            
            // 1. Update speed scaling (every minute)
            UpdateSpeedScaling();
            
            // 2. Update HP scaling (every 5 minutes)
            if (minutesPassed >= lastHPScaleTime + 5f)
            {
                UpdateHPScaling();
                lastHPScaleTime = Mathf.Floor(minutesPassed / 5f) * 5f;
            }
            
            // 3. Update attack scaling (every 5 minutes, staggered with HP)
            if (minutesPassed >= lastAttackScaleTime + 5f)
            {
                UpdateAttackScaling();
                lastAttackScaleTime = Mathf.Floor(minutesPassed / 5f) * 5f;
            }
        }
    }
    
    void UpdateSpeedScaling()
    {
        // Speed increases 5% per minute, but capped at player's current max speed
        float targetSpeedMultiplier = 1f + (minutesPassed * speedIncreasePerMinute);
        
        // Apply the multiplier
        currentSpeedMultiplier = targetSpeedMultiplier;
        
        Debug.Log($"Speed Scaling: {minutesPassed:F1} min → {currentSpeedMultiplier:F2}x multiplier");
    }
    
    void UpdateHPScaling()
    {
        hpScaleCount++;
        currentHPMultiplier += hpIncreasePer5Minutes;
        
        Debug.Log($"HP SCALED! #{hpScaleCount}: +{hpIncreasePer5Minutes:P0} → Total: {currentHPMultiplier:F2}x HP");
    }
    
    void UpdateAttackScaling()
    {
        attackScaleCount++;
        currentAttackMultiplier += attackIncreasePer5Minutes;
        
        Debug.Log($"ATTACK SCALED! #{attackScaleCount}: +{attackIncreasePer5Minutes:P0} → Total: {currentAttackMultiplier:F2}x DMG");
    }
    
    // Public methods for enemies to get scaled values
    public float GetScaledSpeed(float baseSpeed)
    {
        float scaledSpeed = baseSpeed * currentSpeedMultiplier;
        
        // Cap at player's current max speed (enemies shouldn't be faster than player can handle)
        if (playerController != null)
        {
            scaledSpeed = Mathf.Min(scaledSpeed, playerCurrentMaxSpeed);
        }
        
        return scaledSpeed;
    }
    
    public int GetScaledHP(int baseHP)
    {
        return Mathf.RoundToInt(baseHP * currentHPMultiplier);
    }
    
    public int GetScaledAttack(int baseAttack)
    {
        return Mathf.RoundToInt(baseAttack * currentAttackMultiplier);
    }
    
    public float GetScaledBaseDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * currentAttackMultiplier);
    }
    
    // Reset for new game
    public void ResetScaling()
    {
        gameTime = 0f;
        minutesPassed = 0f;
        currentSpeedMultiplier = 1f;
        currentHPMultiplier = 1f;
        currentAttackMultiplier = 1f;
        lastHPScaleTime = 0f;
        lastAttackScaleTime = 0f;
        hpScaleCount = 0;
        attackScaleCount = 0;
        
        Debug.Log("Enemy scaling reset to defaults");
    }
}