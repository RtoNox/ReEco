using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BaseTrigger : MonoBehaviour
{
    [Header("Base Settings")]
    public string interactionKey = "E";
    public GameObject interactionHint;
    
    [Header("UI References")]
    public GameObject upgradePanel;
    public Text coinsText;
    public Text sellValueText;
    public Text sellSummaryText;
    
    [Header("Upgrade Buttons")]
    public Button speedButton;
    public Text speedLevelText;
    public Text speedCostText;
    
    public Button healthButton;
    public Text healthLevelText;
    public Text healthCostText;
    
    public Button attackButton;
    public Text attackLevelText;
    public Text attackCostText;
    
    public Button baseButton;
    public Text baseLevelText;
    public Text baseCostText;
    
    [Header("Base Health System")]
    public BaseHealth baseHealth; // Reference to the BaseHealth component
    public Text baseHealthText; // UI text to show base health
    
    [Header("Upgrade Stats")]
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
    
    // Upgrade levels (RESETS EACH RUN)
    private int speedLevel = 0;
    private int healthLevel = 0;
    private int attackLevel = 0;
    private int baseLevel = 0;
    
    // Player's original stats (to calculate bonuses)
    private float originalPlayerSpeed = 0f;
    private int originalPlayerHealth = 0;
    private int originalPlayerAttack = 0;

    void Start()
    {
        // RESET everything on start
        ResetAllUpgrades();
        
        // Hide UI
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (interactionHint != null) interactionHint.SetActive(false);
        
        // Get BaseHealth component if not assigned
        if (baseHealth == null)
        {
            baseHealth = GetComponent<BaseHealth>();
        }
        
        Debug.Log("BaseTrigger: All upgrades reset for new run");
        
        // Start coroutine to update base health UI
        StartCoroutine(UpdateBaseHealthUICoroutine());
    }

    void ResetAllUpgrades()
    {
        // Reset upgrade levels
        speedLevel = 0;
        healthLevel = 0;
        attackLevel = 0;
        baseLevel = 0;
        
        // Reset base health through BaseHealth component
        if (baseHealth != null)
        {
            baseHealth.ResetBaseHealth();
        }
        
        // Store original player stats (when player enters base)
        // We'll get these when player first interacts with base
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
            
            // Store original player stats if not stored yet
            StoreOriginalPlayerStats();
            
            // Update sell value display
            UpdateSellValueDisplay();
            
            // Update base health display
            UpdateBaseHealthUI();
        }
    }

    void StoreOriginalPlayerStats()
    {
        if (playerController != null && originalPlayerSpeed == 0)
        {
            originalPlayerSpeed = playerController.moveSpeed;
        }
        
        if (playerHealth != null && originalPlayerHealth == 0)
        {
            originalPlayerHealth = playerHealth.maxHealth;
        }
        
        if (playerAttack != null && originalPlayerAttack == 0)
        {
            originalPlayerAttack = playerAttack.attackDamage;
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
        UpdateBaseHealthUI();
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        Time.timeScale = 1f;
        
        if (upgradePanel != null) upgradePanel.SetActive(false);
        
        if (playerController != null)
            playerController.enabled = true;
        
        if (playerInRange && interactionHint != null)
            interactionHint.SetActive(true);
    }

    // =================== UPGRADE SYSTEM ===================
    void UpdateUI()
    {
        // Update coins
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
        
        // Update Base UI
        if (baseLevelText != null) baseLevelText.text = $"Lv.{baseLevel}";
        if (baseCostText != null) baseCostText.text = $"{baseCost}";
        if (baseButton != null) 
        {
            baseButton.interactable = CanAfford(baseCost);
            baseButton.onClick.RemoveAllListeners();
            baseButton.onClick.AddListener(() => BuyUpgrade(3, baseCost));
        }
        
        // Update base health display
        UpdateBaseHealthUI();
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
                    // Reset to original + bonuses
                    playerController.moveSpeed = originalPlayerSpeed + (speedLevel * speedIncrease);
                    Debug.Log($"Speed Lv.{speedLevel}: {playerController.moveSpeed:F1} (+{speedIncrease})");
                }
                break;
                
            case 1: // Health
                healthLevel++;
                if (playerHealth != null)
                {
                    playerHealth.maxHealth = originalPlayerHealth + (healthLevel * healthIncrease);
                    playerHealth.Heal(healthIncrease);
                    Debug.Log($"Health Lv.{healthLevel}: {playerHealth.maxHealth} (+{healthIncrease})");
                }
                break;
                
            case 2: // Attack
                attackLevel++;
                if (playerAttack != null)
                {
                    playerAttack.attackDamage = originalPlayerAttack + (attackLevel * attackIncrease);
                    Debug.Log($"Attack Lv.{attackLevel}: {playerAttack.attackDamage} (+{attackIncrease})");
                }
                break;
                
            case 3: // Base Health
                baseLevel++;
                if (baseHealth != null)
                {
                    baseHealth.IncreaseMaxHealth(baseHealthIncrease);
                    Debug.Log($"Base Health Lv.{baseLevel}: {baseHealth.maxHealth} (+{baseHealthIncrease})");
                }
                break;
        }
        
        UpdateUI();
    }

    // =================== BASE HEALTH UI ===================
    void UpdateBaseHealthUI()
    {
        if (baseHealthText != null && baseHealth != null)
        {
            baseHealthText.text = $"Base Health: {baseHealth.currentHealth}/{baseHealth.maxHealth}";
            
            // Color code based on health percentage
            float healthPercent = (float)baseHealth.currentHealth / baseHealth.maxHealth;
            if (healthPercent > 0.6f)
                baseHealthText.color = Color.green;
            else if (healthPercent > 0.3f)
                baseHealthText.color = Color.yellow;
            else
                baseHealthText.color = Color.red;
        }
    }
    
    IEnumerator UpdateBaseHealthUICoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Update every 0.5 seconds
            
            if (baseHealth != null && (playerInRange || isMenuOpen))
            {
                UpdateBaseHealthUI();
            }
        }
    }

    // =================== RESET FOR NEW RUN ===================
    // Call this when player dies or new run starts
    public void ResetForNewRun()
    {
        Debug.Log($"Resetting {gameObject.name} for new run");
        
        // Reset upgrade levels
        speedLevel = 0;
        healthLevel = 0;
        attackLevel = 0;
        baseLevel = 0;
        
        // Reset base health
        if (baseHealth != null)
        {
            baseHealth.ResetBaseHealth();
        }
        
        // Reset original stats (will be recaptured when player enters)
        originalPlayerSpeed = 0f;
        originalPlayerHealth = 0;
        originalPlayerAttack = 0;
        
        // Update UI if menu is open
        if (isMenuOpen)
        {
            UpdateUI();
            UpdateBaseHealthUI();
        }
    }
    
    // =================== BASE DAMAGE HANDLING ===================
    // These methods can be called by enemies to damage the base
    
    public void DamageBase(int damage)
    {
        if (baseHealth != null)
        {
            baseHealth.TakeDamage(damage);
            UpdateBaseHealthUI();
        }
    }
    
    public bool IsBaseAlive()
    {
        return baseHealth != null && baseHealth.currentHealth > 0;
    }
    
    public Transform GetBaseTransform()
    {
        return transform;
    }
    
    public int GetBaseHealth()
    {
        return baseHealth != null ? baseHealth.currentHealth : 0;
    }
    
    public int GetBaseMaxHealth()
    {
        return baseHealth != null ? baseHealth.maxHealth : 0;
    }

    // =================== OTHER METHODS ===================
    void UpdateSellValueDisplay()
    {
        if (sellValueText == null || playerInventory == null) return;
        
        int totalValue = 0;
        foreach (var slot in playerInventory.inventory)
        {
            totalValue += slot.totalValue;
        }
        
        sellValueText.text = $"Sell value: {totalValue}";
    }

    public void SellItems()
    {
        if (playerInventory == null) return;
        
        int totalValue = 0;
        foreach (var slot in playerInventory.inventory)
        {
            totalValue += slot.totalValue;
        }
        
        if (totalValue > 0)
        {
            playerInventory.SellAllItems();
            
            if (sellSummaryText != null)
            {
                sellSummaryText.text = $"Sold items for {totalValue} coins!";
                StartCoroutine(ClearAfterDelay(sellSummaryText, 3f));
            }
            
            UpdateSellValueDisplay();
            UpdateUI();
        }
    }

    System.Collections.IEnumerator ClearAfterDelay(Text text, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (text != null)
            text.text = "";
    }
}