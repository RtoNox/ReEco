using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootData
{
    public string lootName;
    public LootTier lootTier = LootTier.Common;
    public int minValue = 1;
    public int maxValue = 5;
    public GameObject lootPrefab;
    public float dropChance = 1f;
    public float spawnWeight = 1f; // Higher weight = more likely to spawn
    
    public int GetRandomValue()
    {
        return Random.Range(minValue, maxValue + 1);
    }
    
    public int GetTierMultiplier()
    {
        switch (lootTier)
        {
            case LootTier.Common: return 1;
            case LootTier.Rare: return 3;
            case LootTier.Epic: return 8;
            default: return 1;
        }
    }
    
    public int GetCalculatedValue()
    {
        return GetRandomValue() * GetTierMultiplier();
    }
}