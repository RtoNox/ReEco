using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentObject : MonoBehaviour, IDamageable, ILootable
{
    [Header("Environment Object Settings")]
    public int health = 20;
    public LootData environmentLoot;
    public GameObject destroyEffect;
    
    private bool isDestroyed = false;
    
    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        
        health -= damage;
        
        if (health <= 0)
        {
            DestroyObject();
        }
    }
    
    public void Die()
    {
        DestroyObject();
    }
    
    public bool IsAlive()
    {
        return !isDestroyed && health > 0;
    }
    
    // ILootable implementation
    public LootData GetLootData()
    {
        return environmentLoot;
    }
    
    public void OnLootCollected()
    {
        // Optional implementation
    }
    
    private void DestroyObject()
    {
        isDestroyed = true;
        
        // Drop environment loot
        DropLoot();
        
        // Visual effects
        if (destroyEffect != null)
        {
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
    
    private void DropLoot()
    {
        if (environmentLoot != null && environmentLoot.lootPrefab != null)
        {
            GameObject droppedLoot = Instantiate(environmentLoot.lootPrefab, transform.position, Quaternion.identity);
            
            // Set environment value (normal value)
            CollectibleItem collectible = droppedLoot.GetComponent<CollectibleItem>();
            if (collectible != null)
            {
                collectible.lootTier = environmentLoot.lootTier;
                collectible.value = environmentLoot.GetRandomValue(); // Normal value
            }
        }
    }
}