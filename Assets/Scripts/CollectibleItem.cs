using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleItem : MonoBehaviour, ICollectible
{
    [Header("Collectible Settings")]
    public LootTier lootTier = LootTier.Common;
    public string itemName = "Unknown Item";
    public int value = 1;
    public bool isEnvironmentSpawn = false; // Track if spawned from environment
    public GameObject collectEffect;
    public AudioClip collectSound;
    
    [Header("Visual Settings")]
    public float floatAmplitude = 0.5f;
    public float floatSpeed = 2f;
    public Color commonColor = Color.white;
    public Color rareColor = Color.blue;
    public Color epicColor = Color.yellow;
    
    private Vector3 startPosition;
    private bool isCollected = false;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateAppearance();
        
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
        }
    }
    
    void Update()
    {
        if (!isCollected)
        {
            // Floating animation
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Slow rotation for visual appeal
            transform.Rotate(0, 0, 10 * Time.deltaTime);
        }
    }
    
    private void UpdateAppearance()
    {
        if (spriteRenderer != null)
        {
            switch (lootTier)
            {
                case LootTier.Common:
                    spriteRenderer.color = commonColor;
                    break;
                case LootTier.Rare:
                    spriteRenderer.color = rareColor;
                    break;
                case LootTier.Epic:
                    spriteRenderer.color = epicColor;
                    break;
            }
        }
    }
    
    // ICollectible implementation
    public void Collect(GameObject collector)
    {
        if (isCollected) return;
        isCollected = true;
        
        PlayerInventory inventory = collector.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddItem(this);
        }
        
        // Visual and audio effects
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        string source = isEnvironmentSpawn ? "Environment" : "Enemy";
        Debug.Log($"Collected {lootTier} {itemName} from {source} worth {value} coins!");
        Destroy(gameObject);
    }
    
    public int GetValue()
    {
        return value;
    }
    
    public LootTier GetLootTier()
    {
        return lootTier;
    }
    
    public string GetItemName()
    {
        return itemName;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            Collect(other.gameObject);
        }
    }
}