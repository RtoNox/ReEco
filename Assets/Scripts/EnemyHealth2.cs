using System.Collections;
using UnityEngine;

public class EnemyHealth2 : MonoBehaviour, IDamageable, ITargetable, ILootable
{
    [Header("Base Health (Unscaled)")]
    public int baseMaxHealth = 25;
    
    [Header("Current Stats (Scaled)")]
    public int maxHealth;
    public int currentHealth;
    
    [Header("Enemy2 Settings")]
    public Team team = Team.Enemy;
    public GameObject deathEffect;
    
    [Header("Loot Settings - Enemy2 Drops")]
    public LootData[] possibleLoot;
    public float lootDropChance = 0.75f;
    public LootManager lootManager;
    
    private bool isDead = false;
    
    void Start()
    {
        // Apply scaling
        ApplyScaling();
        
        if (lootManager == null)
        {
            lootManager = FindObjectOfType<LootManager>();
        }
    }
    
    void Update()
    {
        // Update scaling in real-time
        if (EnemyScalingManager.Instance != null && !isDead)
        {
            int newMaxHealth = EnemyScalingManager.Instance.GetScaledHP(baseMaxHealth);
            if (newMaxHealth != maxHealth)
            {
                float healthPercent = (float)currentHealth / maxHealth;
                maxHealth = newMaxHealth;
                currentHealth = Mathf.RoundToInt(maxHealth * healthPercent);
                Debug.Log($"Enemy2 HP updated: {currentHealth}/{maxHealth} ({healthPercent:P0})");
            }
        }
    }
    
    void ApplyScaling()
    {
        if (EnemyScalingManager.Instance != null)
        {
            maxHealth = EnemyScalingManager.Instance.GetScaledHP(baseMaxHealth);
        }
        else
        {
            maxHealth = baseMaxHealth;
        }
        
        currentHealth = maxHealth;
    }
    
    // ================= IDAMAGEABLE =================
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
    
    // ================= ITARGETABLE =================
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
    
    // ================= ILOOTABLE =================
    public LootData GetLootData()
    {
        if (possibleLoot.Length == 0) return null;
        return possibleLoot[Random.Range(0, possibleLoot.Length)];
    }
    
    public void OnLootCollected()
    {
        // Optional
    }
    
    // ================= LOOT DROP =================
    private void DropLoot()
    {
        float roll = Random.Range(0f, 1f);
        if (roll > lootDropChance) return;
        
        if (lootManager == null)
        {
            lootManager = FindObjectOfType<LootManager>();
            if (lootManager == null) return;
        }
        
        LootData lootData = lootManager.GetRandomLoot();
        if (lootData == null || lootData.lootPrefab == null) return;
        
        Vector3 dropPosition = transform.position;
        dropPosition.x += Random.Range(-0.5f, 0.5f);
        dropPosition.y += Random.Range(-0.5f, 0.5f);
        
        GameObject lootObject = Instantiate(
            lootData.lootPrefab,
            dropPosition,
            Quaternion.identity
        );
        
        CollectibleItem collectible = lootObject.GetComponent<CollectibleItem>();
        if (collectible != null)
        {
            int baseValue = lootData.GetCalculatedValue();
            collectible.lootTier = lootData.lootTier;
            collectible.value = baseValue * 2;
            collectible.itemName = lootData.lootName;
            collectible.isEnvironmentSpawn = false;
        }
    }
    
    // ================= DAMAGE FLASH =================
    private IEnumerator DamageFlash()
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