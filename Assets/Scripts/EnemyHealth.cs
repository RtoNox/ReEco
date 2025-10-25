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
    public float lootDropChance = 0.8f; // 80% chance to drop loot
    
    private bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
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
        
        // Drop loot before dying
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
        // Check if enemy drops loot
        if (Random.Range(0f, 1f) <= lootDropChance)
        {
            LootManager lootManager = FindObjectOfType<LootManager>();
            if (lootManager != null)
            {
                LootData lootToDrop = lootManager.GetRandomLoot();
                if (lootToDrop != null && lootToDrop.lootPrefab != null)
                {
                    Vector3 dropPosition = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
                    GameObject droppedLoot = Instantiate(lootToDrop.lootPrefab, dropPosition, Quaternion.identity);
                    
                    // Set as enemy drop (2x value)
                    CollectibleItem collectible = droppedLoot.GetComponent<CollectibleItem>();
                    if (collectible != null)
                    {
                        collectible.lootTier = lootToDrop.lootTier;
                        collectible.value = lootToDrop.GetCalculatedValue() * 2; // Enemy 2x multiplier
                        collectible.itemName = lootToDrop.lootName;
                        collectible.isEnvironmentSpawn = false; // Mark as enemy drop
                    }
                    
                    Debug.Log($"Enemy dropped {lootToDrop.lootTier} {lootToDrop.lootName} worth {collectible.value} coins!");
                }
            }
        }
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