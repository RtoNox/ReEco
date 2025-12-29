using System.Collections;
using System.Collections.Generic;
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
        
        // Detect all damageable objects in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        bool hitEnemy = false;
        
        foreach (Collider2D hit in hits)
        {
            // Use interface instead of specific component
            IDamageable damageable = hit.GetComponent<IDamageable>();
            ITargetable targetable = hit.GetComponent<ITargetable>();
            
            // Check if it's damageable, targetable, and not on our team
            if (damageable != null && targetable != null && 
                targetable.GetTeam() != Team.Player && 
                damageable.IsAlive())
            {
                // Apply damage
                damageable.TakeDamage(attackDamage);
                Debug.Log($"Hit {hit.name} for {attackDamage} damage!");
                
                // Trigger enemy aggro if it's an enemy
                EnemyAI enemyAI = hit.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.OnHitByPlayer();
                    hitEnemy = true;
                }
            }
        }
        
        // Visual feedback
        StartCoroutine(AttackVisual(hitEnemy));
    }
    
    private System.Collections.IEnumerator AttackVisual(bool hitEnemy)
    {
        SpriteRenderer weapon = attackPoint.GetComponentInChildren<SpriteRenderer>();
        if (weapon != null)
        {
            // Change color based on whether we hit an enemy
            weapon.color = hitEnemy ? Color.red : Color.yellow;
            yield return new WaitForSeconds(0.1f);
            weapon.color = Color.white;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}