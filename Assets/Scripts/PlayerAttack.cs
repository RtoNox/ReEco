using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRate = 0.5f;
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public LayerMask enemyLayers;
    
    [Header("References")]
    public Transform attackPoint;
    
    [Header("Visual Feedback")]
    public GameObject attackCirclePrefab; // Assign a circle sprite GameObject
    public float circleDisplayTime = 0.2f;
    
    private float nextAttackTime = 0f;
    private PlayerController playerController;
    
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        
        if (attackPoint == null)
        {
            GameObject point = new GameObject("AttackPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = new Vector3(0.5f, 0, 0);
            attackPoint = point.transform;
        }
    }
    
    void Update()
    {
        if (Time.time >= nextAttackTime && Input.GetMouseButtonDown(0))
        {
            Attack();
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }
    
    void Attack()
    {
        Debug.Log("Attacking!");
        
        // Get attack direction from player controller
        Vector2 attackDirection = playerController.GetAimDirection();
        attackPoint.position = transform.position + (Vector3)attackDirection * 0.5f;
        
        // Create visual feedback
        StartCoroutine(ShowAttackCircle());
        
        // Detect all damageable objects in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        bool hitEnemy = false;
        
        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            ITargetable targetable = hit.GetComponent<ITargetable>();
            
            if (damageable != null && targetable != null && 
                targetable.GetTeam() != Team.Player && 
                damageable.IsAlive())
            {
                damageable.TakeDamage(attackDamage);
                Debug.Log($"Hit {hit.name} for {attackDamage} damage!");
                
                EnemyAI enemyAI = hit.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.OnHitByPlayer();
                    hitEnemy = true;
                }
            }
        }
        
        // Weapon color feedback
        StartCoroutine(WeaponColorFeedback(hitEnemy));
    }
    
    private IEnumerator ShowAttackCircle()
    {
        if (attackCirclePrefab != null)
        {
            // Create the circle
            GameObject circle = Instantiate(attackCirclePrefab, attackPoint.position, Quaternion.identity);
            
            // Scale it to match attack range
            float scale = attackRange * 2; // Assuming sprite is 1 unit diameter
            circle.transform.localScale = new Vector3(scale, scale, 1);
            
            // Fade out effect
            SpriteRenderer circleRenderer = circle.GetComponent<SpriteRenderer>();
            if (circleRenderer != null)
            {
                Color originalColor = circleRenderer.color;
                float timer = 0;
                
                while (timer < circleDisplayTime)
                {
                    timer += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, timer / circleDisplayTime);
                    circleRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    yield return null;
                }
            }
            else
            {
                // Just wait and destroy if no sprite renderer
                yield return new WaitForSeconds(circleDisplayTime);
            }
            
            Destroy(circle);
        }
    }
    
    private IEnumerator WeaponColorFeedback(bool hitEnemy)
    {
        SpriteRenderer weapon = attackPoint.GetComponentInChildren<SpriteRenderer>();
        if (weapon != null)
        {
            weapon.color = hitEnemy ? Color.red : Color.yellow;
            yield return new WaitForSeconds(0.1f);
            weapon.color = Color.white;
        }
    }
}