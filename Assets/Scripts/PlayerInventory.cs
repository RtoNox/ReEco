using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public LootTier lootTier;
        public string itemName;
        public int quantity;
        public int totalValue;
        public int fromEnvironment; // Count from environment spawns
        public int fromEnemies;    // Count from enemy drops
    }

    [Header("Inventory")]
    public List<InventorySlot> inventory = new List<InventorySlot>();
    public int coins = 0;
    
    [Header("UI Reference")]
    public UnityEngine.UI.Text coinsText; // Assign in inspector
    
    public void AddItem(CollectibleItem item)
    {
        int value = item.GetValue();
        LootTier tier = item.GetLootTier();
        string name = item.GetItemName();
        
        // Find existing slot or create new one
        InventorySlot slot = inventory.Find(s => s.itemName == name && s.lootTier == tier);
        if (slot != null)
        {
            slot.quantity++;
            slot.totalValue += value;
            
            // Track source
            if (item.isEnvironmentSpawn)
                slot.fromEnvironment++;
            else
                slot.fromEnemies++;
        }
        else
        {
            slot = new InventorySlot
            {
                lootTier = tier,
                itemName = name,
                quantity = 1,
                totalValue = value,
                fromEnvironment = item.isEnvironmentSpawn ? 1 : 0,
                fromEnemies = item.isEnvironmentSpawn ? 0 : 1
            };
            inventory.Add(slot);
        }
        
        string source = item.isEnvironmentSpawn ? "environment" : "enemy";
        Debug.Log($"Added {tier} {name} from {source} to inventory. Total: {slot.quantity} (Value: {slot.totalValue})");
        UpdateUI();
    }
    
    public void SellAllItems()
    {
        int totalEarned = 0;
        
        foreach (InventorySlot slot in inventory)
        {
            totalEarned += slot.totalValue;
            string sourceInfo = $"(Env: {slot.fromEnvironment}, Enemy: {slot.fromEnemies})";
            Debug.Log($"Sold {slot.quantity} {slot.lootTier} {slot.itemName} {sourceInfo} for {slot.totalValue} coins");
        }
        
        coins += totalEarned;
        inventory.Clear();
        
        UpdateUI();
        Debug.Log($"Sold all items! Earned {totalEarned} coins. Total coins: {coins}");
    }
    
    public void SellSpecificType(LootTier tier, string itemName = "")
    {
        List<InventorySlot> slotsToSell;
        
        if (string.IsNullOrEmpty(itemName))
        {
            // Sell all items of this tier
            slotsToSell = inventory.FindAll(s => s.lootTier == tier);
        }
        else
        {
            // Sell specific item
            slotsToSell = inventory.FindAll(s => s.lootTier == tier && s.itemName == itemName);
        }
        
        int totalEarned = 0;
        foreach (InventorySlot slot in slotsToSell)
        {
            totalEarned += slot.totalValue;
            Debug.Log($"Sold {slot.quantity} {slot.lootTier} {slot.itemName} for {slot.totalValue} coins");
            inventory.Remove(slot);
        }
        
        coins += totalEarned;
        UpdateUI();
        Debug.Log($"Sold items! Earned {totalEarned} coins. Total coins: {coins}");
    }
    
    private void UpdateUI()
    {
        if (coinsText != null)
        {
            coinsText.text = $"Coins: {coins}";
            
            // Optional: Show inventory count
            int totalItems = 0;
            foreach (InventorySlot slot in inventory)
            {
                totalItems += slot.quantity;
            }
            coinsText.text += $"\nItems: {totalItems}";
        }
    }
    
    // Call this when player reaches base
    public void ReturnToBase()
    {
        SellAllItems();
    }
    
    public int GetTotalItems()
    {
        int total = 0;
        foreach (InventorySlot slot in inventory)
        {
            total += slot.quantity;
        }
        return total;
    }
    
    public int GetItemsByTier(LootTier tier)
    {
        int total = 0;
        foreach (InventorySlot slot in inventory)
        {
            if (slot.lootTier == tier)
            {
                total += slot.quantity;
            }
        }
        return total;
    }
    
    void Start()
    {
        UpdateUI();
    }
}