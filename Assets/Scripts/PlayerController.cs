using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Sprites")]
    public Sprite playerFront;
    public Sprite playerBack;
    public Sprite playerLeft;
    public Sprite playerRight;
    private SpriteRenderer spriteRenderer;
    
    [Header("References")]
    public Rigidbody2D rb;
    public Transform weaponPivot;
    public Animator animator;
    
    private Vector2 movement;
    private Vector2 aimDirection;
    private bool isDashing = false;
    private bool canDash = true;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        spriteRenderer.sprite = playerFront;
    }

    void Update()
    {
        HandleInput();
        HandleAim();
        HandleDash();
        UpdatePlayerSpriteBasedOnAim();
    }
    
    void FixedUpdate()
    {
        if (!isDashing)
        {
            HandleMovement();
        }
        else
        {
            HandleDashMovement();
        }
    }
    
    void UpdatePlayerSpriteBasedOnAim()
    {
        if (aimDirection.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            
            if (angle < 0) angle += 360;
            
            if (angle >= 45 && angle < 135)
            {
                // Aiming up
                spriteRenderer.sprite = playerBack;
            }
            else if (angle >= 135 && angle < 225)
            {
                spriteRenderer.sprite = playerLeft;
            }
            else if (angle >= 225 && angle < 315)
            {
                spriteRenderer.sprite = playerFront;
            }
            else
            {
                spriteRenderer.sprite = playerRight;
            }
        }
    }

    void HandleInput()
    {
        // Keyboard movement only
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movement = new Vector2(horizontal, vertical).normalized;
    }
    
    void HandleMovement()
    {
        rb.velocity = movement * moveSpeed;
        
        // Update animator if available
        if (animator != null)
        {
            animator.SetFloat("Speed", movement.magnitude);
            animator.SetFloat("Horizontal", movement.x);
            animator.SetFloat("Vertical", movement.y);
        }
    }
    
    void HandleAim()
    {
        // Mouse aim only
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;
        aimDirection = (mousePosition - weaponPivot.position).normalized;
        
        // Rotate weapon to face aim direction
        if (aimDirection.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            weaponPivot.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    
    void HandleDash()
    {
        // Keyboard dash only
        bool dashInput = Input.GetKeyDown(KeyCode.LeftShift);
        
        if (dashInput && canDash && movement.magnitude > 0.1f)
        {
            StartCoroutine(PerformDash());
        }
    }
    
    void HandleDashMovement()
    {
        rb.velocity = movement * dashSpeed;
    }
    
    IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;
        
        // Optional: Trigger dash animation
        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }
        
        // Optional: Add invincibility during dash
        // GetComponent<Collider2D>().enabled = false;
        
        // Dash for the specified duration
        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
        
        // Optional: Restore collision
        // GetComponent<Collider2D>().enabled = true;
        
        // Cooldown before next dash
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    
    // Public methods for other scripts to access player state
    public bool IsDashing()
    {
        return isDashing;
    }
    
    public Vector2 GetAimDirection()
    {
        return aimDirection;
    }
    
    public Vector2 GetMovement()
    {
        return movement;
    }
    
    // Visual feedback in editor
    void OnDrawGizmos()
    {
        // Draw aim direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)aimDirection * 2f);
        
        // Draw movement direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)movement * 1.5f);
    }

    void UpdateAnimatorDirection()
    {
        if (movement.magnitude > 0.1f)
        {
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                // Moving mostly horizontally
                if (movement.x > 0)
                    animator.SetTrigger("MoveRight");
                else
                    animator.SetTrigger("MoveLeft");
            }
            else
            {
                // Moving mostly vertically
                if (movement.y > 0)
                    animator.SetTrigger("MoveBack");
                else
                    animator.SetTrigger("MoveFront");
            }
        }
        else
        {
            animator.SetTrigger("Idle");
        }
    }

    void HandlePlayerRotation()
    {
        // Make player face movement direction when moving
        if (movement.magnitude > 0.1f)
        {
            // Or face aim direction instead:
            // float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            // transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // Simple sprite flip based on horizontal movement
            if (Mathf.Abs(movement.x) > 0.1f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Sign(movement.x) * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }
}