using UnityEngine;

/// <summary>
/// 2D Platformer character that auto-runs.
/// The VR "God" player helps this character by spawning platforms,
/// blocking enemies, etc.
/// Supports VR-controlled movement via SetMoveDirection.
/// Uses Rigidbody2D for true 2D physics.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlatformerCharacter2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float jumpForce = 8f;
    public int direction = 1; // 1 = right, -1 = left

    [Header("Ground Check")]
    public float groundCheckDist = 0.1f;
    public LayerMask groundMask = ~0; // Everything by default

    [Header("Health")]
    public int maxLives = 3;
    public float damageFlashDuration = 0.5f;
    public float invincibleDuration = 1.5f;

    Rigidbody2D rb;
    bool grounded;
    int lives;
    Vector3 startPosition;
    Color originalColor;
    SpriteRenderer spriteRenderer;
    float invincibleTimer;
    bool vrControlled; // true when VR input is actively steering

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Start()
    {
        lives = maxLives;
        startPosition = transform.localPosition;
    }

    void FixedUpdate()
    {
        // Movement: auto-run in current direction
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // Ground check via raycast downward
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        Vector2 origin = (Vector2)transform.position + box.offset - new Vector2(0, box.size.y * 0.5f * transform.localScale.y);
        float width = box.size.x * transform.localScale.x * 0.8f;

        bool hitCenter = Physics2D.Raycast(origin, Vector2.down, groundCheckDist, groundMask);
        bool hitLeft = Physics2D.Raycast(origin - new Vector2(width * 0.5f, 0), Vector2.down, groundCheckDist, groundMask);
        bool hitRight = Physics2D.Raycast(origin + new Vector2(width * 0.5f, 0), Vector2.down, groundCheckDist, groundMask);

        grounded = hitCenter || hitLeft || hitRight;
    }

    void Update()
    {
        // Invincibility timer
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;

            // Flash effect
            if (spriteRenderer != null)
            {
                float flash = Mathf.Sin(Time.time * 20f) > 0f ? 0.3f : 1f;
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, flash);
            }

            if (invincibleTimer <= 0f && spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Flip direction when hitting a wall (only in auto-run mode)
        if (!vrControlled && col.contactCount > 0)
        {
            Vector2 normal = col.GetContact(0).normal;
            if (Mathf.Abs(normal.x) > 0.7f)
            {
                Flip();
            }
        }
    }

    /// <summary>
    /// Set movement direction from VR input.
    /// dir < 0 = left, dir > 0 = right, dir == 0 = auto-run (keep current direction).
    /// </summary>
    public void SetMoveDirection(float dir)
    {
        if (dir < 0f)
        {
            vrControlled = true;
            if (direction != -1)
            {
                direction = -1;
                ApplyFlipScale();
            }
        }
        else if (dir > 0f)
        {
            vrControlled = true;
            if (direction != 1)
            {
                direction = 1;
                ApplyFlipScale();
            }
        }
        else
        {
            // No input: fall back to auto-run
            vrControlled = false;
        }
    }

    /// <summary>God commands the hero to jump.</summary>
    public void GodJump()
    {
        if (grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    /// <summary>God flips the hero's direction.</summary>
    public void Flip()
    {
        direction *= -1;
        ApplyFlipScale();
    }

    void ApplyFlipScale()
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * direction;
        transform.localScale = s;
    }

    /// <summary>
    /// Called when hero takes damage from an enemy.
    /// Flashes red and respawns at start if out of lives.
    /// </summary>
    public void TakeDamage()
    {
        // Ignore if invincible
        if (invincibleTimer > 0f) return;

        lives--;
        Debug.Log($"[Hero] Took damage! Lives remaining: {lives}");

        // Flash red
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        invincibleTimer = invincibleDuration;

        if (lives <= 0)
        {
            // Respawn
            lives = maxLives;
            transform.localPosition = startPosition;
            rb.linearVelocity = Vector2.zero;
            Debug.Log("[Hero] Out of lives! Respawning...");
        }
    }

    public bool IsGrounded() => grounded;
    public int GetLives() => lives;
}
