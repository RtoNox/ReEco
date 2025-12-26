using UnityEngine;
using UnityEngine.UI;

public class BaseTrigger : MonoBehaviour
{
    [Header("Base Settings")]
    public string interactionKey = "E";
    public GameObject interactionHint;
    
    [Header("UI References")]
    public GameObject upgradePanel;
    public Text coinsText;
    public Text sellValueText; // This shows total value of items in inventory
    public Text sellSummaryText; // This shows "Sold X items for Y coins"
    
    [Header("Speed Upgrade")]
    public Button speedButton;
    public Text speedNameText;
    public Text speedLevelText;
    public Text speedCostText;
    
    [Header("Health Upgrade")]
    public Button healthButton;
    public Text healthNameText;
    public Text healthLevelText;
    public Text healthCostText;
    
    [Header("Attack Upgrade")]
    public Button attackButton;
    public Text attackNameText;
    public Text attackLevelText;
    public Text attackCostText;
    
    [Header("Base Health Upgrade")]
    public Button baseButton;
    public Text baseNameText;
    public Text baseLevelText;
    public Text baseCostText;
    
    [Header("Upgrade Settings")]
    public float speedIncrease = 0.5f;
    public int healthIncrease = 25;
    public int attackIncrease = 5;
    public int baseHealthIncrease = 50;
    
    public int speedBaseCost = 50;
    public int healthBaseCost = 100;
    public int attackBaseCost = 75;
    public int baseHealthBaseCost = 150;
    
    public float costMultiplier = 1.5f;
    
    // Player references
    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private PlayerAttack playerAttack;
    private bool isMenuOpen = false;
    
    // Upgrade levels
    private int speedLevel = 0;
    private int healthLevel = 0;
    private int attackLevel = 0;
    private int baseLevel = 0;
    
    // Base health
    private int baseMaxHealth = 500;
    private int currentBaseHealth = 500;

    void Start()
    {
        LoadGameData();
        
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (interactionHint != null) interactionHint.SetActive(false);
        
        // Initialize sell value display
        UpdateSellValueDisplay();
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey) && playerInventory != null)
        {
            ToggleMenu();
        }
        
        if (isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            playerController = other.GetComponent<PlayerController>();
            playerHealth = other.GetComponent<PlayerHealth>();
            playerAttack = other.GetComponent<PlayerAttack>();
            
            if (interactionHint != null)
                interactionHint.SetActive(true);
            
            // Update sell value display when player enters base
            UpdateSellValueDisplay();
            
            // Auto-sell items (if you want this)
            // SellItems();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            if (interactionHint != null)
                interactionHint.SetActive(false);
            
            if (isMenuOpen)
                CloseMenu();
        }
    }

    // =================== MENU MANAGEMENT ===================
    void ToggleMenu()
    {
        if (isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    void OpenMenu()
    {
        isMenuOpen = true;
        Time.timeScale = 0f;
        
        if (upgradePanel != null) upgradePanel.SetActive(true);
        if (interactionHint != null) interactionHint.SetActive(false);
        
        if (playerController != null)
            playerController.enabled = false;
        
        UpdateUI();
    }

    void CloseMenu()
    {
        isMenuOpen = false;
        Time.timeScale = 1f;
        
        if (upgradePanel != null) upgradePanel.SetActive(false);
        
        if (playerController != null)
            playerController.enabled = true;
        
        if (playerInRange && interactionHint != null)
            interactionHint.SetActive(true);
    }

    // =================== SELL VALUE DISPLAY ===================
    void UpdateSellValueDisplay()
    {
        if (sellValueText == null || playerInventory == null) return;
        
        int totalSellValue = CalculateTotalSellValue();
        sellValueText.text = $"Sell value: {totalSellValue}";
    }
    
    int CalculateTotalSellValue()
    {
        int totalValue = 0;
        
        // Loop through all inventory slots and sum their totalValue
        foreach (var slot in playerInventory.inventory)
        {
            totalValue += slot.totalValue;
        }
        
        return totalValue;
    }

    // =================== SELLING SYSTEM ===================
    public void SellItems()
    {
        if (playerInventory == null) return;
        
        // Store the total value before selling for the summary
        int totalSellValue = CalculateTotalSellValue();
        int itemCount = playerInventory.GetTotalItems();
        int beforeCoins = playerInventory.coins;
        
        if (totalSellValue > 0)
        {
            // Call your existing SellAllItems method
            playerInventory.SellAllItems();
            
            int earned = playerInventory.coins - beforeCoins;
            
            // Update sell summary
            if (sellSummaryText != null)
            {
                sellSummaryText.text = $"Sold {itemCount} items for {earned} coins!";
                StartCoroutine(ClearAfterDelay(sellSummaryText, 3f));
            }
            
            Debug.Log($"Sold {itemCount} items worth {totalSellValue} coins. Earned: {earned}");
            
            // Update sell value display (should be 0 now)
            UpdateSellValueDisplay();
        }
        else
        {
            if (sellSummaryText != null)
            {
                sellSummaryText.text = "No items to sell";
                StartCoroutine(ClearAfterDelay(sellSummaryText, 2f));
            }
        }
        
        // Update coins display
        UpdateUI();
    }
    
    System.Collections.IEnumerator ClearAfterDelay(Text text, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (text != null)
            text.text = "";
    }

    // =================== UPGRADE SYSTEM ===================
    void UpdateUI()
    {
        // Update coins display
        if (coinsText != null && playerInventory != null)
            coinsText.text = $"Coins: {playerInventory.coins}";
        
        // Calculate current costs
        int speedCost = CalculateCost(speedBaseCost, speedLevel);
        int healthCost = CalculateCost(healthBaseCost, healthLevel);
        int attackCost = CalculateCost(attackBaseCost, attackLevel);
        int baseCost = CalculateCost(baseHealthBaseCost, baseLevel);
        
        // Update Speed UI
        if (speedLevelText != null) speedLevelText.text = $"Lv.{speedLevel}";
        if (speedCostText != null) speedCostText.text = $"{speedCost}";
        if (speedButton != null) 
        {
            speedButton.interactable = CanAfford(speedCost);
            speedButton.onClick.RemoveAllListeners();
            speedButton.onClick.AddListener(() => BuyUpgrade(0, speedCost));
        }
        
        // Update Health UI
        if (healthLevelText != null) healthLevelText.text = $"Lv.{healthLevel}";
        if (healthCostText != null) healthCostText.text = $"{healthCost}";
        if (healthButton != null) 
        {
            healthButton.interactable = CanAfford(healthCost);
            healthButton.onClick.RemoveAllListeners();
            healthButton.onClick.AddListener(() => BuyUpgrade(1, healthCost));
        }
        
        // Update Attack UI
        if (attackLevelText != null) attackLevelText.text = $"Lv.{attackLevel}";
        if (attackCostText != null) attackCostText.text = $"{attackCost}";
        if (attackButton != null) 
        {
            attackButton.interactable = CanAfford(attackCost);
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => BuyUpgrade(2, attackCost));
        }
        
        // Update Base Health UI
        if (baseLevelText != null) baseLevelText.text = $"Lv.{baseLevel}";
        if (baseCostText != null) baseCostText.text = $"{baseCost}";
        if (baseButton != null) 
        {
            baseButton.interactable = CanAfford(baseCost);
            baseButton.onClick.RemoveAllListeners();
            baseButton.onClick.AddListener(() => BuyUpgrade(3, baseCost));
        }
    }

    int CalculateCost(int baseCost, int level)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, level));
    }

    bool CanAfford(int cost)
    {
        return playerInventory != null && playerInventory.coins >= cost;
    }

    void BuyUpgrade(int upgradeType, int cost)
    {
        if (!CanAfford(cost)) return;
        
        playerInventory.coins -= cost;
        
        switch (upgradeType)
        {
            case 0: // Speed
                speedLevel++;
                if (playerController != null)
                {
                    playerController.moveSpeed += speedIncrease;
                    Debug.Log($"Speed Lv.{speedLevel}: {playerController.moveSpeed:F1}");
                }
                break;
                
            case 1: // Health
                healthLevel++;
                if (playerHealth != null)
                {
                    playerHealth.maxHealth += healthIncrease;
                    playerHealth.Heal(healthIncrease);
                    Debug.Log($"Health Lv.{healthLevel}: {playerHealth.maxHealth}");
                }
                break;
                
            case 2: // Attack
                attackLevel++;
                if (playerAttack != null)
                {
                    playerAttack.attackDamage += attackIncrease;
                    Debug.Log($"Attack Lv.{attackLevel}: {playerAttack.attackDamage}");
                }
                break;
                
            case 3: // Base Health
                baseLevel++;
                baseMaxHealth += baseHealthIncrease;
                currentBaseHealth += baseHealthIncrease;
                Debug.Log($"Base Health Lv.{baseLevel}: {baseMaxHealth}");
                break;
        }
        
        SaveGameData();
        UpdateUI();
    }

    // =================== SAVE/LOAD ===================
    void LoadGameData()
    {
        speedLevel = PlayerPrefs.GetInt("SpeedLevel", 0);
        healthLevel = PlayerPrefs.GetInt("HealthLevel", 0);
        attackLevel = PlayerPrefs.GetInt("AttackLevel", 0);
        baseLevel = PlayerPrefs.GetInt("BaseLevel", 0);
        
        baseMaxHealth = PlayerPrefs.GetInt("BaseMaxHealth", 500);
        currentBaseHealth = PlayerPrefs.GetInt("CurrentBaseHealth", baseMaxHealth);
    }

    void SaveGameData()
    {
        PlayerPrefs.SetInt("SpeedLevel", speedLevel);
        PlayerPrefs.SetInt("HealthLevel", healthLevel);
        PlayerPrefs.SetInt("AttackLevel", attackLevel);
        PlayerPrefs.SetInt("BaseLevel", baseLevel);
        
        PlayerPrefs.SetInt("BaseMaxHealth", baseMaxHealth);
        PlayerPrefs.SetInt("CurrentBaseHealth", currentBaseHealth);
        
        PlayerPrefs.Save();
    }

    // =================== PUBLIC METHODS ===================
    public void TakeBaseDamage(int damage)
    {
        currentBaseHealth -= damage;
        currentBaseHealth = Mathf.Max(0, currentBaseHealth);
        
        if (currentBaseHealth <= 0)
            BaseDestroyed();
        
        PlayerPrefs.SetInt("CurrentBaseHealth", currentBaseHealth);
        PlayerPrefs.Save();
    }

    void BaseDestroyed()
    {
        Debug.Log("Base destroyed!");
    }
    
    // Call this from a Sell Button in your UI
    public void OnSellButtonClicked()
    {
        SellItems();
    }
}