using UnityEngine;

/// <summary>
/// Simple enemy that moves toward the hero and dies when hit by a projectile.
/// Spawns as a red triangle, moves toward hero, damages on contact.
/// </summary>
public class Enemy2D : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 1.5f;
    public int hp = 1;

    [Header("References")]
    public Transform target; // hero

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void Start()
    {
        // Auto-destroy after 30s as cleanup
        Destroy(gameObject, 30f);
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        // Flip sprite to face movement direction
        if (dir.x != 0f)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir.x > 0 ? 1f : -1f);
            transform.localScale = s;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hit by projectile
        if (other.CompareTag("Projectile"))
        {
            TakeHit(1);
            return;
        }

        // Contact with hero
        PlatformerCharacter2D hero = other.GetComponent<PlatformerCharacter2D>();
        if (hero != null)
        {
            hero.TakeDamage();
            Destroy(gameObject);
        }
    }

    public void TakeHit(int damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Factory: creates an enemy GameObject with all required components.
    /// </summary>
    public static GameObject Create(Vector3 position, Transform target, Transform parent = null)
    {
        GameObject go = new GameObject("Enemy");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * 0.6f;
        go.tag = "Enemy";

        // Red triangle sprite
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Geo2D.Triangle();
        sr.color = new Color(0.9f, 0.15f, 0.15f);
        sr.sortingOrder = 8;

        // Physics
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Enemy2D enemy = go.AddComponent<Enemy2D>();
        enemy.target = target;

        return go;
    }
}
