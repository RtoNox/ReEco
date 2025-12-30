using System.Collections;
using UnityEngine;

public class EnemyAI2 : MonoBehaviour
{
    [Header("Base Stats (Unscaled)")]
    public float baseMoveSpeed = 1.8f;
    public int baseMaxHealth = 25;
    public int baseDamage = 8;
    public int baseAttackDamage = 8;
    
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
    public int baseDamageToBase = 15;
    public float baseDetectionRange = 15f;
    
    [Header("Target Switching")]
    public float playerAggroDuration = 5f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 8f;
    public float dashDuration = 0.25f;
    public float dashCooldownMin = 5f;
    public float dashCooldownMax = 10f;
    
    // References
    private Transform player;
    private Transform baseTarget;
    private Transform currentTarget;
    private float lastAttackTime;
    private float playerAggroTimer;
    private bool isAggroOnPlayer;
    private bool baseIsVisible;
    private bool isDashing = false;
    private SpriteRenderer spriteRenderer;
    
    // Health reference
    private EnemyHealth2 enemyHealth;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        baseTarget = GameObject.FindGameObjectWithTag("Base")?.transform;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent<EnemyHealth2>();
        
        currentTarget = player;
        
        // Apply scaling
        ApplyScaling();
        
        StartCoroutine(DashRoutine());
    }
    
    void Update()
    {
        // Update scaling in real-time
        UpdateScaling();
        
        UpdateAggro();
        CheckBaseVisibility();
        DetermineTarget();
        
        if (isDashing || currentTarget == null) return;
        
        float distance = Vector2.Distance(transform.position, currentTarget.position);
        float range = (currentTarget == baseTarget) ? baseAttackRange : attackRange;
        
        if (distance > range)
        {
            Vector2 dir = (currentTarget.position - transform.position).normalized;
            transform.position += (Vector3)dir * currentMoveSpeed * Time.deltaTime;
            FaceDirection(dir.x);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
        
        transform.rotation = Quaternion.identity;
    }
    
    void UpdateScaling()
    {
        if (EnemyScalingManager.Instance != null)
        {
            currentMoveSpeed = EnemyScalingManager.Instance.GetScaledSpeed(baseMoveSpeed);
            currentDamage = EnemyScalingManager.Instance.GetScaledAttack(baseDamage);
            currentAttackDamage = EnemyScalingManager.Instance.GetScaledAttack(baseAttackDamage);
        }
    }
    
    void ApplyScaling()
    {
        if (EnemyScalingManager.Instance != null)
        {
            currentMoveSpeed = EnemyScalingManager.Instance.GetScaledSpeed(baseMoveSpeed);
            currentDamage = EnemyScalingManager.Instance.GetScaledAttack(baseDamage);
            currentAttackDamage = EnemyScalingManager.Instance.GetScaledAttack(baseAttackDamage);
            
            // Update health if EnemyHealth2 component exists
            if (enemyHealth != null)
            {
                currentMaxHealth = EnemyScalingManager.Instance.GetScaledHP(baseMaxHealth);
                enemyHealth.baseMaxHealth = baseMaxHealth;
            }
        }
        else
        {
            currentMoveSpeed = baseMoveSpeed;
            currentMaxHealth = baseMaxHealth;
            currentDamage = baseDamage;
            currentAttackDamage = baseAttackDamage;
        }
    }
    
    // ================= AGGRO =================
    void UpdateAggro()
    {
        if (!isAggroOnPlayer) return;
        
        playerAggroTimer -= Time.deltaTime;
        if (playerAggroTimer <= 0)
            isAggroOnPlayer = false;
    }
    
    public void OnHitByPlayer()
    {
        isAggroOnPlayer = true;
        playerAggroTimer = playerAggroDuration;
        currentTarget = player;
    }
    
    // ================= DASH =================
    IEnumerator DashRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(dashCooldownMin, dashCooldownMax));
            if (!isDashing)
                StartCoroutine(Dash());
        }
    }
    
    IEnumerator Dash()
    {
        isDashing = true;
        
        float direction = Random.value < 0.5f ? -1f : 1f;
        FaceDirection(direction);
        
        float timer = 0f;
        while (timer < dashDuration)
        {
            transform.position += Vector3.right * direction * dashSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
        
        isDashing = false;
    }
    
    // ================= TARGETING =================
    void CheckBaseVisibility()
    {
        baseIsVisible = false;
        if (baseTarget == null || Camera.main == null) return;
        
        float dist = Vector2.Distance(transform.position, baseTarget.position);
        if (dist > baseDetectionRange) return;
        
        Vector3 vp = Camera.main.WorldToViewportPoint(baseTarget.position);
        baseIsVisible = vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1;
    }
    
    void DetermineTarget()
    {
        if (isAggroOnPlayer && player != null)
        {
            currentTarget = player;
            return;
        }
        
        if (baseIsVisible && baseTarget != null)
        {
            currentTarget = baseTarget;
            return;
        }
        
        currentTarget = player;
    }
    
    // ================= ATTACK =================
    void Attack()
    {
        if (currentTarget == null) return;
        
        if (currentTarget.CompareTag("Player"))
        {
            IDamageable d = currentTarget.GetComponent<IDamageable>();
            if (d != null && d.IsAlive())
                d.TakeDamage(currentAttackDamage);
        }
        else if (currentTarget.CompareTag("Base"))
        {
            BaseHealth bh = currentTarget.GetComponent<BaseHealth>();
            if (bh != null)
                bh.TakeDamage(Mathf.RoundToInt(currentAttackDamage * 1.5f));
        }
    }
    
    // ================= COLLISION =================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerWeapon") || 
            collision.gameObject.CompareTag("Player"))
        {
            OnHitByPlayer();
        }
        
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsAlive())
        {
            int collisionDamage = (collision.gameObject.CompareTag("Base")) ? 
                Mathf.RoundToInt(currentDamage * 1.5f) : 
                Mathf.RoundToInt(currentDamage * 0.5f);
            damageable.TakeDamage(collisionDamage);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerWeapon"))
        {
            OnHitByPlayer();
        }
    }
    
    // ================= VISUAL =================
    void FaceDirection(float x)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = x < 0;
    }
}