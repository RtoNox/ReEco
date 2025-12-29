using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BaseHealth : MonoBehaviour
{
    [Header("Base Health Settings")]
    public int maxHealth = 500;
    public int currentHealth;
    public bool isInvulnerable = false;
    public float invulnerabilityDuration = 1f;
    
    [Header("Regeneration Settings")]
    public bool canRegenerate = true;
    public float regenRate = 1f; // Health per second
    public float regenDelay = 3f; // Seconds after taking damage before regen starts
    public float regenTickInterval = 1f; // How often regen is applied
    
    [Header("Enemy Base Attack Settings")]
    public float enemyBaseAttackRange = 2f; // Range at which enemies can attack base
    public int enemyBaseDamage = 20; // Damage enemies do to base
    public float enemyBaseAttackCooldown = 1.5f; // How often enemies can attack base
    public LayerMask enemyLayer; // Layer to detect enemies
    
    [Header("Visual Feedback")]
    public Image healthBarFill;
    public GameObject damageEffect;
    public GameObject healEffect;
    public AudioClip damageSound;
    public AudioClip healedSound;
    public AudioClip destroyedSound;
    
    [Header("UI References")]
    public Text healthText;
    public Text regenStatusText;
    
    [Header("Game Over Settings")]
    public GameObject gameOverPanel; // Assign a UI panel in the inspector
    public Text gameOverText;
    public Button restartButton;
    public Button mainMenuButton;
    public string gameOverMessage = "BASE DESTROYED!";
    
    // References
    private Animator animator;
    private AudioSource audioSource;
    private BaseTrigger baseTrigger;
    private bool isDead = false;
    
    // Regen variables
    private float timeSinceLastDamage = 0f;
    private float regenTimer = 0f;
    private bool isRegenerating = false;
    
    // Enemy attack tracking
    private Collider2D[] nearbyEnemies = new Collider2D[20];
    private float lastEnemyCheckTime = 0f;
    private float enemyCheckInterval = 0.5f;

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get components
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        baseTrigger = GetComponent<BaseTrigger>();
        
        // Initialize enemy layer if not set
        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }
        
        // Setup Game Over UI
        InitializeGameOverUI();
        
        // Update UI
        UpdateHealthUI();
        UpdateRegenStatusUI();
        
        // Start regen coroutine
        StartCoroutine(RegenerationCoroutine());
    }
    
    void InitializeGameOverUI()
    {
        // Hide game over panel at start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Setup restart button
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
        
        // Setup main menu button
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
        
        // Set game over text
        if (gameOverText != null)
        {
            gameOverText.text = gameOverMessage;
        }
    }
    
    void Update()
    {
        if (canRegenerate && !isDead)
        {
            // Track time since last damage
            timeSinceLastDamage += Time.deltaTime;
            
            // Check if we should start regenerating
            if (timeSinceLastDamage >= regenDelay && currentHealth < maxHealth)
            {
                isRegenerating = true;
            }
            else
            {
                isRegenerating = false;
            }
            
            // Update regen timer
            regenTimer += Time.deltaTime;
            
            // Update regen status UI
            UpdateRegenStatusUI();
        }
        
        // Check for nearby enemies periodically
        if (!isDead && Time.time - lastEnemyCheckTime > enemyCheckInterval)
        {
            CheckForEnemies();
            lastEnemyCheckTime = Time.time;
        }
    }
    
    void CheckForEnemies()
    {
        // Find all enemies near the base
        int enemyCount = Physics2D.OverlapCircleNonAlloc(
            transform.position, 
            enemyBaseAttackRange, 
            nearbyEnemies, 
            enemyLayer
        );
        
        // Damage the base for each nearby enemy
        if (enemyCount > 0 && Time.time % enemyBaseAttackCooldown < 0.1f) // Rough cooldown check
        {
            int totalDamage = enemyCount * enemyBaseDamage;
            if (totalDamage > 0)
            {
                TakeDamage(totalDamage);
            }
        }
    }
    
    IEnumerator RegenerationCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenTickInterval);
            
            if (canRegenerate && isRegenerating && !isDead && currentHealth < maxHealth)
            {
                Heal((int)regenRate); // Heal 1 HP per tick
            }
        }
    }
    
    public void TakeDamage(int damage)
    {
        // Check if invulnerable or already dead
        if (isInvulnerable || isDead || currentHealth <= 0)
            return;
        
        // Reset regen timer
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        regenTimer = 0f;
        
        // Apply damage
        currentHealth -= damage;
        
        // Clamp health
        if (currentHealth < 0)
            currentHealth = 0;
        
        // Visual and audio feedback
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }
        
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Shake effect (optional)
        StartCoroutine(ShakeBase());
        
        // Update UI
        UpdateHealthUI();
        UpdateRegenStatusUI();
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Start invulnerability period
            StartCoroutine(InvulnerabilityPeriod());
        }
    }
    
    IEnumerator InvulnerabilityPeriod()
    {
        isInvulnerable = true;
        
        // Flash effect (optional)
        StartCoroutine(FlashEffect(Color.red));
        
        yield return new WaitForSeconds(invulnerabilityDuration);
        
        isInvulnerable = false;
    }
    
    IEnumerator FlashEffect(Color flashColor)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        int flashes = 3;
        float flashDuration = 0.1f;
        
        for (int i = 0; i < flashes; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
    }
    
    IEnumerator ShakeBase()
    {
        Vector3 originalPosition = transform.position;
        float shakeDuration = 0.2f;
        float shakeMagnitude = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = originalPosition.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = originalPosition.y + Random.Range(-shakeMagnitude, shakeMagnitude);
            
            transform.position = new Vector3(x, y, originalPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
    }
    
    void UpdateHealthUI()
    {
        // Update health bar
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = $"Base Health: {currentHealth}/{maxHealth}";
        }
    }
    
    void UpdateRegenStatusUI()
    {
        if (regenStatusText == null) return;
        
        if (isDead)
        {
            regenStatusText.text = "BASE DESTROYED";
            regenStatusText.color = Color.red;
        }
        else if (currentHealth >= maxHealth)
        {
            regenStatusText.text = "Health: Full";
            regenStatusText.color = Color.green;
        }
        else if (isRegenerating)
        {
            float nextHealIn = regenTickInterval - (regenTimer % regenTickInterval);
            regenStatusText.text = $"Regenerating: +{regenRate}/s (Next: {nextHealIn:F1}s)";
            regenStatusText.color = new Color(0.5f, 1f, 0.5f); // Light green
        }
        else if (timeSinceLastDamage < regenDelay)
        {
            float timeUntilRegen = regenDelay - timeSinceLastDamage;
            regenStatusText.text = $"Regen in: {timeUntilRegen:F1}s";
            regenStatusText.color = Color.yellow;
        }
        else
        {
            regenStatusText.text = "Ready to Regenerate";
            regenStatusText.color = Color.white;
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log("Base destroyed! Game Over!");
        
        if (destroyedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destroyedSound);
        }
        
        if (animator != null)
        {
            animator.SetTrigger("Destroy");
        }
        
        UpdateRegenStatusUI();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBaseDestroyed();
        }
        
        ShowGameOver();
    }

    void ShowGameOver()
    {
        Time.timeScale = 0f;
        
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }
        
        DisableAllEnemies();
    }
    
    void DisableAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.enabled = false;
            }
            
            // Stop enemy movement
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.velocity = Vector2.zero;
                enemyRb.isKinematic = true;
            }
        }
    }
    
    // Game Over UI Button Methods
    void RestartGame()
    {
        // Restart the current scene
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
    
    void GoToMainMenu()
    {
        // Load main menu scene (assumes scene 0 is main menu)
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    
    public void Heal(int amount)
    {
        if (isDead || currentHealth >= maxHealth) return;
        
        int previousHealth = currentHealth;
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        
        int actualHealAmount = currentHealth - previousHealth;
        
        // Visual feedback for healing
        if (actualHealAmount > 0 && healEffect != null)
        {
            Instantiate(healEffect, transform.position, Quaternion.identity);
        }
        
        if (actualHealAmount > 0 && healedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healedSound, 0.5f);
        }
        
        // Flash green when healing
        if (actualHealAmount > 0)
        {
            StartCoroutine(FlashEffect(Color.green));
        }
        
        UpdateHealthUI();
    }
    
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount; // Heal by the same amount
        UpdateHealthUI();
        
        Debug.Log($"Base max health increased to {maxHealth}");
    }
    
    // Call this from BaseTrigger when base health is upgraded
    public void OnBaseHealthUpgraded(int increaseAmount)
    {
        IncreaseMaxHealth(increaseAmount);
    }
    
    // Optional: Add methods to modify regen rate
    public void SetRegenRate(float newRate)
    {
        regenRate = newRate;
    }
    
    public void SetRegenDelay(float newDelay)
    {
        regenDelay = newDelay;
    }
    
    public void EnableRegeneration(bool enable)
    {
        canRegenerate = enable;
        UpdateRegenStatusUI();
    }
    
    public void ResetBaseHealth()
    {
        currentHealth = maxHealth = 500;
        isDead = false;
        isInvulnerable = false;
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        
        // Re-enable collider if disabled
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = true;
        
        UpdateHealthUI();
        UpdateRegenStatusUI();
        
        Debug.Log("Base health reset to 500");
    }

    // For debugging - visualize attack range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyBaseAttackRange);
    }
}