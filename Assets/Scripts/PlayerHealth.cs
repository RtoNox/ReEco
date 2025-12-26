using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable, IHealable, ITargetable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isInvincible = false;
    public float invincibilityDuration = 1f;
    public Team team = Team.Player;

    [Header("Events")]
    public UnityEvent<int> OnDamageTaken;
    public UnityEvent<int> OnHeal;
    public UnityEvent OnDeath;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        OnDamageTaken?.Invoke(damage);

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
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
        
        GetComponent<PlayerController>().enabled = false;
        Debug.Log("Player Died!");
    }

    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }

    // IHealable implementation
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        Debug.Log($"Player healed for {amount}! Health: {currentHealth}/{maxHealth}");
        OnHeal?.Invoke(amount);
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

    private System.Collections.IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        for (int i = 0; i < 5; i++)
        {
            if (sprite != null) sprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (sprite != null) sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }

        isInvincible = false;
    }

    public void Respawn()
    {
        currentHealth = maxHealth;
        isDead = false;
        GetComponent<PlayerController>().enabled = true;
        Debug.Log("Player Respawned!");
    }
}