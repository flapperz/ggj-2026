using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Test script: press Space to spawn a random 2D shape (square, circle, triangle,
/// diamond, hexagon) that falls with 2D gravity.
/// Attach to the GameWorld2D root object.
/// Uses New Input System.
/// </summary>
public class SpawnAndDrop : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnHeight = 5f;
    public float spawnRangeX = 4f;
    public float squareSize = 0.5f;

    [Header("Auto Spawn")]
    public bool autoSpawn = false;
    public float autoInterval = 1f;
    public int maxSquares = 50;

    int count;
    float timer;

    void Update()
    {
        // New Input System: check Space key
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Drop();
        }

        if (autoSpawn && count < maxSquares)
        {
            timer += Time.deltaTime;
            if (timer >= autoInterval)
            {
                timer = 0f;
                Drop();
            }
        }
    }

    public void Drop()
    {
        float x = transform.position.x + Random.Range(-spawnRangeX, spawnRangeX);
        float y = transform.position.y + spawnHeight;
        Vector3 pos = new Vector3(x, y, 0f);

        Color col = new Color(Random.Range(0.3f, 1f), Random.Range(0.3f, 1f), Random.Range(0.3f, 1f));
        float size = Random.Range(0.3f, 0.7f);

        // Pick a random shape
        int shape = Random.Range(0, 6);
        Sprite sprite;
        string shapeName;
        bool useCircleCollider = false;

        switch (shape)
        {
            case 0:
                sprite = Geo2D.Square();
                shapeName = "Square";
                break;
            case 1:
                sprite = Geo2D.Circle();
                shapeName = "Circle";
                useCircleCollider = true;
                break;
            case 2:
                sprite = Geo2D.Triangle();
                shapeName = "Triangle";
                break;
            case 3:
                sprite = Geo2D.Diamond();
                shapeName = "Diamond";
                break;
            case 4:
                sprite = Geo2D.Hexagon();
                shapeName = "Hexagon";
                break;
            default:
                sprite = Geo2D.Star();
                shapeName = "Star";
                break;
        }

        GameObject go = new GameObject(shapeName + "_" + count);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = col;
        sr.sortingOrder = 3;

        // Physics
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.mass = size; // Heavier = bigger

        if (useCircleCollider)
            go.AddComponent<CircleCollider2D>();
        else
            go.AddComponent<BoxCollider2D>();

        // Small random spin for fun
        rb.angularVelocity = Random.Range(-90f, 90f);

        count++;
        Destroy(go, 20f);
    }
}
