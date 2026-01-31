using UnityEngine;

/// <summary>
/// Marks a 2D platform as draggable by the VR god player's grip.
/// Kinematic Rigidbody2D pushes the hero on contact.
/// Horizontal-only movement clamped to min/max X bounds.
/// Visual feedback: normal (cyan), hovered (light cyan), dragged (yellow).
/// Subtle bob animation when idle to distinguish from static platforms.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class DraggablePlatform2D : MonoBehaviour
{
    [Header("Drag Bounds")]
    public float minX = -7f;
    public float maxX = 7f;

    [Header("Visual Feedback")]
    public Color normalColor = new Color(0f, 0.9f, 0.9f);
    public Color hoveredColor = new Color(0.6f, 1f, 1f);
    public Color draggedColor = new Color(1f, 0.9f, 0.2f);

    [Header("Bob Animation")]
    public float bobAmplitude = 0.06f;
    public float bobFrequency = 1.5f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    float baseY;
    float targetX;
    bool isDragged;
    bool isHovered;

    public bool IsDragged => isDragged;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        baseY = transform.localPosition.y;
        targetX = transform.localPosition.x;
    }

    void FixedUpdate()
    {
        float x = Mathf.Clamp(targetX, minX, maxX);
        float y = baseY;

        if (!isDragged)
        {
            y += Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        }

        Vector3 localPos = new Vector3(x, y, transform.localPosition.z);
        Vector3 worldPos = transform.parent != null
            ? transform.parent.TransformPoint(localPos)
            : localPos;

        rb.MovePosition(worldPos);
    }

    void Update()
    {
        if (sr == null) return;

        if (isDragged)
            sr.color = draggedColor;
        else if (isHovered)
            sr.color = hoveredColor;
        else
            sr.color = normalColor;
    }

    public void SetLocalX(float x)
    {
        targetX = Mathf.Clamp(x, minX, maxX);
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
    }

    public void SetDragged(bool dragged)
    {
        isDragged = dragged;
    }
}
