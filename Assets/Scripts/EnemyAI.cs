using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public int damage = 10;
    public float attackCooldown = 1f;
    
    [Header("Base Targeting")]
    public float baseAttackRange = 1.5f;
    public int baseDamage = 20;
    public float baseDetectionRange = 15f; // How far enemy can detect base
    
    [Header("Target Switching")]
    public float playerAggroDuration = 5f; // How long to chase player after being hit
    public bool showDebugRanges = false;
    
    // Target references
    private Transform player;
    private Transform baseTarget;
    private IDamageable currentTarget;
    private Transform currentTransformTarget;
    private float lastAttackTime;
    
    // State tracking
    private float playerAggroTimer = 0f;
    private bool isAggroOnPlayer = false;
    private bool baseIsVisible = false;
    
    void Start()
    {
        // Find player
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Find base
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null)
        {
            baseTarget = baseObj.transform;
        }
        
        // Set initial target (will be updated in Update)
        currentTransformTarget = player;
    }
    
    void Update()
    {
        // Update aggro timer
        if (isAggroOnPlayer)
        {
            playerAggroTimer -= Time.deltaTime;
            if (playerAggroTimer <= 0f)
            {
                isAggroOnPlayer = false;
            }
        }
        
        // Check if base is visible on screen
        CheckBaseVisibility();
        
        // Determine current target based on priority
        DetermineTarget();
        
        // If no valid target, do nothing
        if (currentTransformTarget == null) return;
        
        // Move toward current target
        float distanceToTarget = Vector2.Distance(transform.position, currentTransformTarget.position);
        float currentAttackRange = (currentTransformTarget == baseTarget) ? baseAttackRange : attackRange;
        
        if (distanceToTarget > currentAttackRange)
        {
            // Move toward target
            Vector2 direction = (currentTransformTarget.position - transform.position).normalized;
            transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Attack current target
            AttackCurrentTarget();
            lastAttackTime = Time.time;
        }
    }
    
    void CheckBaseVisibility()
    {
        baseIsVisible = false;
        
        if (baseTarget == null) return;
        
        // Check if base is within detection range
        float distanceToBase = Vector2.Distance(transform.position, baseTarget.position);
        if (distanceToBase > baseDetectionRange) return;
        
        // Check if base is within screen bounds (simplified check)
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(baseTarget.position);
        baseIsVisible = (screenPoint.x >= 0 && screenPoint.x <= 1 && 
                        screenPoint.y >= 0 && screenPoint.y <= 1);
    }
    
    void DetermineTarget()
    {
        // Priority 1: Player if aggro is active
        if (isAggroOnPlayer && player != null)
        {
            currentTransformTarget = player;
            return;
        }
        
        // Priority 2: Base if visible on screen
        if (baseIsVisible && baseTarget != null)
        {
            currentTransformTarget = baseTarget;
            return;
        }
        
        // Priority 3: Default to player
        currentTransformTarget = player;
    }
    
    void AttackCurrentTarget()
    {
        if (currentTransformTarget == player)
        {
            // Attack player
            IDamageable damageable = currentTransformTarget.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                damageable.TakeDamage(damage);
                Debug.Log("Enemy attacked player!");
            }
        }
        else if (currentTransformTarget == baseTarget)
        {
            // Attack base
            BaseHealth baseHealth = baseTarget.GetComponent<BaseHealth>();
            if (baseHealth != null)
            {
                baseHealth.TakeDamage(baseDamage);
                Debug.Log("Enemy attacked base!");
            }
        }
    }
    
    // Call this when enemy is hit by player
    public void OnHitByPlayer()
    {
        isAggroOnPlayer = true;
        playerAggroTimer = playerAggroDuration;
        
        // Immediately switch to chasing player
        if (player != null)
        {
            currentTransformTarget = player;
        }
        
        Debug.Log("Enemy aggro on player!");
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if hit by player attack
        if (collision.gameObject.CompareTag("PlayerWeapon") || 
            collision.gameObject.CompareTag("Player"))
        {
            OnHitByPlayer();
        }
        
        // Deal damage on collision
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsAlive())
        {
            // Determine damage based on target
            int collisionDamage = (collision.gameObject.CompareTag("Base")) ? baseDamage : damage / 2;
            damageable.TakeDamage(collisionDamage);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit by player attack (for trigger-based attacks)
        if (other.CompareTag("PlayerWeapon"))
        {
            OnHitByPlayer();
        }
    }
    
    // For debugging
    void OnDrawGizmosSelected()
    {
        if (showDebugRanges)
        {
            // Draw player attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw base attack range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, baseAttackRange);
            
            // Draw base detection range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, baseDetectionRange);
            
            // Draw line to current target
            if (currentTransformTarget != null)
            {
                Gizmos.color = (isAggroOnPlayer) ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, currentTransformTarget.position);
            }
        }
    }
}