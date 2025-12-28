using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable, ITargetable, ILootable
{
    [Header("Enemy Settings")]
    public int maxHealth = 50;
    public int currentHealth;
    public Team team = Team.Enemy;
    public GameObject deathEffect;
    
    [Header("Loot Settings - Enemy Drops (2x Value)")]
    public LootData[] possibleLoot;
    public float lootDropChance = 0.9f;
    public LootManager lootManager;
    
    private bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        if (lootManager == null)
        {
            lootManager = FindObjectOfType<LootManager>();
        }
    }
    
    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        StartCoroutine(DamageFlash());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Die()
    {
        isDead = true;
        
        DropLoot();
        
        // Death effects
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
    
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }
    
    // ITargetable implementation
    public Transform GetTransform()
    {
        return transform;
    }
    
    public Team GetTeam()
    {
        return team;
    }
    
    public bool IsTargetable()
    {
        return !isDead;
    }
    
    // ILootable implementation
    public LootData GetLootData()
    {
        if (possibleLoot.Length == 0) return null;
        return possibleLoot[Random.Range(0, possibleLoot.Length)];
    }
    
    public void OnLootCollected()
    {
        // Called when loot is collected (optional)
    }
    
    private void DropLoot()
    {
        Debug.Log($"=== DROP LOOT STARTED for {gameObject.name} ===");
        
        float roll = Random.Range(0f, 1f);
        Debug.Log($"Drop chance check: Rolled {roll:F2}, Need <= {lootDropChance}: {(roll <= lootDropChance ? "PASS" : "FAIL")}");
        
        if (roll > lootDropChance)
        {
            Debug.Log("Failed drop chance roll - no loot dropped");
            return;
        }
        
        if (lootManager == null)
        {
            Debug.LogError("LootManager is null! Cannot drop loot.");
            lootManager = FindObjectOfType<LootManager>();
            Debug.Log($"After FindObjectOfType: LootManager = {lootManager != null}");
            
            if (lootManager == null)
            {
                // Try to find it another way
                GameObject lootManagerObj = GameObject.Find("LootManager");
                if (lootManagerObj != null)
                {
                    lootManager = lootManagerObj.GetComponent<LootManager>();
                    Debug.Log($"Found by name: LootManager = {lootManager != null}");
                }
            }
            
            if (lootManager == null) 
            {
                Debug.LogError("STILL no LootManager found! Check if it's in the scene.");
                return;
            }
        }
        
        Debug.Log($"LootManager found: {lootManager.gameObject.name}");
        
        // Get loot data
        LootData lootData = lootManager.GetRandomLoot();
        Debug.Log($"GetRandomLoot returned: {(lootData != null ? lootData.lootName : "NULL")}");
        
        if (lootData == null)
        {
            Debug.LogError("LootManager.GetRandomLoot() returned null!");
            return;
        }
        
        Debug.Log($"LootData: {lootData.lootName} (Tier: {lootData.lootTier})");
        Debug.Log($"Loot prefab: {(lootData.lootPrefab != null ? "Exists" : "NULL!")}");
        
        if (lootData.lootPrefab == null)
        {
            Debug.LogError("LootData.lootPrefab is null! Check LootManager settings.");
            return;
        }
        
        // Calculate drop position
        Vector3 dropPosition = transform.position;
        dropPosition.x += Random.Range(-0.5f, 0.5f);
        dropPosition.y += Random.Range(-0.5f, 0.5f);
        
        Debug.Log($"Instantiating loot at position: {dropPosition}");
        
        // Instantiate the loot
        GameObject lootObject = Instantiate(lootData.lootPrefab, dropPosition, Quaternion.identity);
        Debug.Log($"Instantiated loot object: {lootObject.name}");
        
        // Get CollectibleItem component
        CollectibleItem collectible = lootObject.GetComponent<CollectibleItem>();
        Debug.Log($"CollectibleItem component: {(collectible != null ? "Found" : "NOT FOUND")}");
        
        if (collectible != null)
        {
            int baseValue = lootData.GetCalculatedValue();
            int enemyValue = baseValue * 2;
            
            collectible.lootTier = lootData.lootTier;
            collectible.value = enemyValue;
            collectible.itemName = lootData.lootName;
            collectible.isEnvironmentSpawn = false;
            
            Debug.Log($"Configured loot: {collectible.lootTier} {collectible.itemName} worth {collectible.value} coins");
        }
        else
        {
            Debug.LogError($"Loot prefab {lootData.lootPrefab.name} doesn't have CollectibleItem component!");
        }
        
        Debug.Log($"=== DROP LOOT COMPLETED ===");
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Color original = sprite.color;
            sprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sprite.color = original;
        }
    }
}