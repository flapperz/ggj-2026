using UnityEngine;

/// <summary>
/// Spawns enemies at screen edges periodically.
/// Attach to the 2D game world root.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public float spawnInterval = 3f;
    public int maxEnemies = 10;
    public float difficultyRampRate = 0.02f; // spawn interval decreases per second
    public float minSpawnInterval = 0.8f;

    [Header("Bounds")]
    public float spawnXEdge = 8f;  // left/right edge X position
    public float spawnYMin = 0.5f;
    public float spawnYMax = 7f;

    [Header("References")]
    public Transform heroTarget;

    float timer;
    float elapsed;

    void Update()
    {
        elapsed += Time.deltaTime;

        // Difficulty ramp: decrease spawn interval over time
        float currentInterval = Mathf.Max(minSpawnInterval, spawnInterval - elapsed * difficultyRampRate);

        timer += Time.deltaTime;
        if (timer >= currentInterval)
        {
            timer = 0f;
            TrySpawn();
        }
    }

    void TrySpawn()
    {
        if (heroTarget == null) return;

        // Count current enemies
        int count = 0;
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Enemy")) count++;
        }
        if (count >= maxEnemies) return;

        // Random edge: left or right
        float x = Random.value > 0.5f ? spawnXEdge : -spawnXEdge;
        float y = Random.Range(spawnYMin, spawnYMax);

        Enemy2D.Create(new Vector3(x, y, 0f), heroTarget, transform);
    }
}
