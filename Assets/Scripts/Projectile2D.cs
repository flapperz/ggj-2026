using UnityEngine;

/// <summary>
/// Projectile spawned by right hand shooting into the screen.
/// Moves in a direction, destroys enemies on contact.
/// </summary>
public class Projectile2D : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 12f;
    public Vector2 direction = Vector2.down;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Auto-destroy after 3s
        Destroy(gameObject, 3f);

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy2D enemy = other.GetComponent<Enemy2D>();
            if (enemy != null)
            {
                enemy.TakeHit(1);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Factory: creates a projectile GameObject with all required components.
    /// </summary>
    public static GameObject Create(Vector2 position, Vector2 direction, Transform parent = null)
    {
        GameObject go = new GameObject("Projectile");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = Vector3.one * 0.3f;
        go.tag = "Projectile";

        // Yellow circle sprite
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Geo2D.Circle();
        sr.color = new Color(1f, 0.9f, 0.2f);
        sr.sortingOrder = 9;

        // Physics - no gravity, not kinematic so triggers work
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Projectile2D proj = go.AddComponent<Projectile2D>();
        proj.direction = direction;

        return go;
    }
}
