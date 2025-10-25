using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LootManager : MonoBehaviour
{
    [Header("Loot Tables")]
    public LootData[] commonLoot;
    public LootData[] rareLoot;
    public LootData[] epicLoot;
    
    [Header("Spawn Settings")]
    public float commonSpawnWeight = 70f;
    public float rareSpawnWeight = 25f;
    public float epicSpawnWeight = 5f;
    
    private Dictionary<LootTier, List<LootData>> lootTables = new Dictionary<LootTier, List<LootData>>();
    
    void Start()
    {
        // Initialize loot tables
        lootTables[LootTier.Common] = new List<LootData>(commonLoot);
        lootTables[LootTier.Rare] = new List<LootData>(rareLoot);
        lootTables[LootTier.Epic] = new List<LootData>(epicLoot);
        
        // Subscribe to tier unlock events
        GameManager.Instance.OnTierUnlocked.AddListener(OnTierUnlocked);
    }
    
    public LootData GetRandomLoot()
    {
        LootTier selectedTier = SelectTier();
        
        // Get available loot for the selected tier
        List<LootData> availableLoot = GetAvailableLootForTier(selectedTier);
        
        if (availableLoot.Count == 0)
        {
            // Fallback to common loot if no loot available for selected tier
            availableLoot = GetAvailableLootForTier(LootTier.Common);
        }
        
        if (availableLoot.Count > 0)
        {
            // Weighted random selection within tier
            float totalWeight = availableLoot.Sum(loot => loot.spawnWeight);
            float randomValue = Random.Range(0f, totalWeight);
            
            float currentWeight = 0f;
            foreach (LootData loot in availableLoot)
            {
                currentWeight += loot.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return loot;
                }
            }
        }
        
        return null;
    }
    
    private LootTier SelectTier()
    {
        float totalWeight = commonSpawnWeight + rareSpawnWeight + epicSpawnWeight;
        float randomValue = Random.Range(0f, totalWeight);
        
        // Adjust weights based on unlocked tiers
        float adjustedCommon = commonSpawnWeight;
        float adjustedRare = GameManager.Instance.IsTierUnlocked(LootTier.Rare) ? rareSpawnWeight : 0f;
        float adjustedEpic = GameManager.Instance.IsTierUnlocked(LootTier.Epic) ? epicSpawnWeight : 0f;
        
        float adjustedTotal = adjustedCommon + adjustedRare + adjustedEpic;
        randomValue = Random.Range(0f, adjustedTotal);
        
        if (randomValue <= adjustedCommon)
            return LootTier.Common;
        else if (randomValue <= adjustedCommon + adjustedRare)
            return LootTier.Rare;
        else
            return LootTier.Epic;
    }
    
    private List<LootData> GetAvailableLootForTier(LootTier tier)
    {
        if (lootTables.ContainsKey(tier) && GameManager.Instance.IsTierUnlocked(tier))
        {
            return lootTables[tier];
        }
        return new List<LootData>();
    }
    
    private void OnTierUnlocked(LootTier newTier)
    {
        Debug.Log($"Loot Manager: {newTier} tier is now available for spawning!");
        
        // Adjust spawn weights when new tiers unlock
        switch (newTier)
        {
            case LootTier.Rare:
                commonSpawnWeight = 60f;
                rareSpawnWeight = 35f;
                epicSpawnWeight = 5f;
                break;
            case LootTier.Epic:
                commonSpawnWeight = 50f;
                rareSpawnWeight = 35f;
                epicSpawnWeight = 15f;
                break;
        }
    }
    
    public List<LootData> GetAllAvailableLoot()
    {
        List<LootData> allLoot = new List<LootData>();
        
        foreach (LootTier tier in System.Enum.GetValues(typeof(LootTier)))
        {
            if (GameManager.Instance.IsTierUnlocked(tier))
            {
                allLoot.AddRange(GetAvailableLootForTier(tier));
            }
        }
        
        return allLoot;
    }
}