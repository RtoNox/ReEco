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
    
    public Transform player;
    private float lastAttackTime;
    private ITargetable target;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        target = player.GetComponent<ITargetable>();
    }
    
    void Update()
    {
        if (player == null || target == null || !target.IsTargetable()) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer > attackRange)
        {
            // Move toward player
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Attack player
            Attack();
            lastAttackTime = Time.time;
        }
    }
    
    void Attack()
    {
        IDamageable damageable = player.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsAlive())
        {
            damageable.TakeDamage(damage);
            Debug.Log("Enemy attacked player!");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsAlive())
        {
            // Simple collision damage
            damageable.TakeDamage(damage / 2);
        }
    }
}