using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Base Stats (Unscaled)")]
    public float baseMoveSpeed = 2f;
    public int baseMaxHealth = 50;
    public int baseDamage = 10;
    public int baseAttackDamage = 10;
    
    [Header("Current Stats (Scaled)")]
    public float currentMoveSpeed;
    public int currentMaxHealth;
    public int currentDamage;
    public int currentAttackDamage;
    
    [Header("AI Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    
    [Header("Base Targeting")]
    public float baseAttackRange = 1.5f;
    public int baseDamageToBase = 20;
    public float baseDetectionRange = 15f;
    
    [Header("Target Switching")]
    public float playerAggroDuration = 5f;
    public bool showDebugRanges = false;
    
    // Target references
    private Transform player;
    private Transform baseTarget;
    private Transform currentTransformTarget;
    private float lastAttackTime;
    
    // State tracking
    private float playerAggroTimer = 0f;
    private bool isAggroOnPlayer = false;
    private bool baseIsVisible = false;
    
    // For sprite flipping
    private SpriteRenderer spriteRenderer;
    
    // Health reference
    private EnemyHealth enemyHealth;
    
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
        
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // Set initial target
        currentTransformTarget = player;
        
        // Apply scaling
        ApplyScaling();
        
        // Ensure no rotation
        LockRotation();
    }
    
    void Update()
    {
        // Update scaling in real-time
        UpdateScaling();
        
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
        
        // Determine current target
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
            transform.position += (Vector3)direction * currentMoveSpeed * Time.deltaTime;
            
            // Face the target
            FaceTarget(direction.x);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Attack current target
            AttackCurrentTarget();
            lastAttackTime = Time.time;
        }
        
        // Constantly lock rotation
        LockRotation();
    }
    
    void UpdateScaling()
    {
        if (EnemyScalingManager.Instance != null)
        {
            // Update speed in real-time
            currentMoveSpeed = EnemyScalingManager.Instance.GetScaledSpeed(baseMoveSpeed);
            
            // Update damage in real-time
            currentDamage = EnemyScalingManager.Instance.GetScaledAttack(baseDamage);
            currentAttackDamage = EnemyScalingManager.Instance.GetScaledAttack(baseAttackDamage);
        }
    }
    
    void ApplyScaling()
    {
        if (EnemyScalingManager.Instance != null)
        {
            // Get scaled values from manager
            currentMoveSpeed = EnemyScalingManager.Instance.GetScaledSpeed(baseMoveSpeed);
            currentDamage = EnemyScalingManager.Instance.GetScaledAttack(baseDamage);
            currentAttackDamage = EnemyScalingManager.Instance.GetScaledAttack(baseAttackDamage);
            
            // Update health if EnemyHealth component exists
            if (enemyHealth != null)
            {
                currentMaxHealth = EnemyScalingManager.Instance.GetScaledHP(baseMaxHealth);
                enemyHealth.baseMaxHealth = baseMaxHealth;
            }
            
            Debug.Log($"Enemy scaled: Speed={currentMoveSpeed:F1}, HP={currentMaxHealth}, DMG={currentDamage}");
        }
        else
        {
            // Use base stats if no scaling manager
            currentMoveSpeed = baseMoveSpeed;
            currentMaxHealth = baseMaxHealth;
            currentDamage = baseDamage;
            currentAttackDamage = baseAttackDamage;
        }
    }
    
    void LockRotation()
    {
        // Force zero rotation - no rotation at all
        transform.rotation = Quaternion.identity;
    }
    
    void FaceTarget(float directionX)
    {
        if (spriteRenderer == null) return;
        
        // Flip sprite based on horizontal direction
        spriteRenderer.flipX = directionX < 0;
    }
    
    void CheckBaseVisibility()
    {
        baseIsVisible = false;
        
        if (baseTarget == null) return;
        
        // Check if base is within detection range
        float distanceToBase = Vector2.Distance(transform.position, baseTarget.position);
        if (distanceToBase > baseDetectionRange) return;
        
        // Check if base is within screen bounds
        if (Camera.main != null)
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(baseTarget.position);
            baseIsVisible = (screenPoint.x >= 0 && screenPoint.x <= 1 && 
                            screenPoint.y >= 0 && screenPoint.y <= 1);
        }
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
                damageable.TakeDamage(currentAttackDamage);
                Debug.Log("Enemy attacked player!");
            }
        }
        else if (currentTransformTarget == baseTarget)
        {
            // Attack base
            BaseHealth baseHealth = baseTarget.GetComponent<BaseHealth>();
            if (baseHealth != null)
            {
                int damageToBase = Mathf.RoundToInt(currentAttackDamage * 1.5f);
                baseHealth.TakeDamage(damageToBase);
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
            int collisionDamage = (collision.gameObject.CompareTag("Base")) ? 
                Mathf.RoundToInt(currentDamage * 1.5f) : 
                Mathf.RoundToInt(currentDamage * 0.5f);
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