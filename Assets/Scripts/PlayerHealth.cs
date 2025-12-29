using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable, IHealable, ITargetable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isInvincible = false;
    public float invincibilityDuration = 1f;
    public Team team = Team.Player;

    [Header("Regeneration Settings")]
    public bool canRegenerate = true;
    public float regenRate = 1f; // Health per second
    public float regenDelay = 3f; // Seconds after taking damage before regen starts
    public float regenTickInterval = 1f; // How often regen is applied
    
    [Header("UI References")]
    public Text healthText; // Assign in inspector
    public Image healthBarFill; // Optional: Health bar
    public Text regenStatusText; // Optional: Shows regen status
    public GameObject damageTextPrefab; // Optional: Floating damage numbers
    public GameObject healEffect; // Optional: Visual effect for healing

    [Header("Events")]
    public UnityEvent<int> OnDamageTaken;
    public UnityEvent<int> OnHeal;
    public UnityEvent OnDeath;

    private bool isDead = false;
    
    // Regen variables
    private float timeSinceLastDamage = 0f;
    private float regenTimer = 0f;
    private bool isRegenerating = false;
    private Coroutine regenCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        // Start regeneration coroutine
        if (canRegenerate)
        {
            regenCoroutine = StartCoroutine(RegenerationCoroutine());
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

    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible) return;
        
        // Reset regen timer
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        regenTimer = 0f;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        OnDamageTaken?.Invoke(damage);

        // Update UI
        UpdateHealthUI();
        UpdateRegenStatusUI();
        
        // Show damage text (optional)
        if (damageTextPrefab != null)
        {
            ShowDamageText(damage);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log("Player Died!");
        OnDeath?.Invoke();
        
        // Update UI to show death
        UpdateHealthUI();
        UpdateRegenStatusUI();
        
        // Stop regeneration
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
        
        // Disable player controller
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = false;
            
        // Optional: Disable player attack
        PlayerAttack pa = GetComponent<PlayerAttack>();
        if (pa != null)
            pa.enabled = false;
        
        Debug.Log("Player Died!");
    }

    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }

    // IHealable implementation
    public void Heal(int amount)
    {
        if (isDead || currentHealth >= maxHealth) return;
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        
        int actualHealAmount = currentHealth - previousHealth;
        
        if (actualHealAmount > 0)
        {
            Debug.Log($"Player healed for {actualHealAmount}! Health: {currentHealth}/{maxHealth}");
            OnHeal?.Invoke(actualHealAmount);
            
            // Update UI
            UpdateHealthUI();
            UpdateRegenStatusUI();
            
            // Optional: Show heal effect
            if (healEffect != null)
            {
                Instantiate(healEffect, transform.position, Quaternion.identity);
            }
            
            // Optional: Show heal text
            if (damageTextPrefab != null)
            {
                ShowHealText(actualHealAmount);
            }
        }
    }

    public bool CanBeHealed()
    {
        return !isDead && currentHealth < maxHealth;
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
    
    // Health UI Methods
    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {currentHealth}/{maxHealth}";
            
            // Color coding based on health percentage
            float healthPercent = (float)currentHealth / maxHealth;
            if (healthPercent > 0.6f)
                healthText.color = Color.green;
            else if (healthPercent > 0.3f)
                healthText.color = Color.yellow;
            else
                healthText.color = Color.red;
        }
        
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }
    
    void UpdateRegenStatusUI()
    {
        if (regenStatusText == null) return;
        
        if (isDead)
        {
            regenStatusText.text = "DEAD";
            regenStatusText.color = Color.red;
        }
        else if (currentHealth >= maxHealth)
        {
            regenStatusText.text = "Full HP";
            regenStatusText.color = Color.green;
        }
        else if (isRegenerating)
        {
            float nextHealIn = regenTickInterval - (regenTimer % regenTickInterval);
            regenStatusText.text = $"Healing: +{regenRate}/s (Next: {nextHealIn:F1}s)";
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
    
    void ShowDamageText(int damage)
    {
        GameObject damageText = Instantiate(damageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        Text textComponent = damageText.GetComponent<Text>();
        if (textComponent != null)
        {
            textComponent.text = $"-{damage}";
            textComponent.color = Color.red;
        }
        
        // Auto-destroy after 1 second
        Destroy(damageText, 1f);
    }
    
    void ShowHealText(int healAmount)
    {
        GameObject healText = Instantiate(damageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        Text textComponent = healText.GetComponent<Text>();
        if (textComponent != null)
        {
            textComponent.text = $"+{healAmount}";
            textComponent.color = Color.green;
        }
        
        // Auto-destroy after 1 second
        Destroy(healText, 1f);
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            for (int i = 0; i < 5; i++)
            {
                sprite.color = new Color(1f, 0.5f, 0.5f, 0.5f); // Red tint with transparency
                yield return new WaitForSeconds(0.1f);
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
        }

        isInvincible = false;
    }

    public void Respawn()
    {
        currentHealth = maxHealth;
        isDead = false;
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        
        // Re-enable player controller
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = true;
            
        // Re-enable player attack
        PlayerAttack pa = GetComponent<PlayerAttack>();
        if (pa != null)
            pa.enabled = true;
        
        // Restart regeneration
        if (canRegenerate && regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(RegenerationCoroutine());
        }
        
        // Update UI
        UpdateHealthUI();
        UpdateRegenStatusUI();
        
        Debug.Log("Player Respawned!");
    }
    
    // Public methods to modify regen
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
        if (enable && regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(RegenerationCoroutine());
        }
        else if (!enable && regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
        UpdateRegenStatusUI();
    }
}