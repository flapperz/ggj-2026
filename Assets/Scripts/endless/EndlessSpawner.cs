using UnityEngine;

public class EndlessSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject enemyPrefab;
    public Transform spawnCenter; // Usually the Player

    [Header("Spawn Settings")]
    public float spawnRadius = 25f; // Radius of the circle perimeter
    public float baseSpawnRate = 2f; // Seconds between spawns at 0 score
    public float minSpawnRate = 0.1f; // Fastest possible spawn rate
    public float difficultyScaling = 0.03f; // How much each score point reduces spawn delay

    private float spawnTimer;

    void Start()
    {
        if (spawnCenter == null)
            spawnCenter = GameObject.FindWithTag("Player")?.transform;
            
        spawnTimer = baseSpawnRate;
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnemyOnCircle();
            ResetTimer();
        }
    }

    private void SpawnEnemyOnCircle()
    {
        if (enemyPrefab == null || spawnCenter == null) return;

        // 1. Get a random angle in radians
        float angle = Random.Range(0f, Mathf.PI * 2f);

        // 2. Calculate position on the perimeter (X = Cos, Y = Sin)
        float x = Mathf.Cos(angle) * spawnRadius;
        float y = Mathf.Sin(angle) * spawnRadius;

        Vector3 spawnOffset = new Vector3(x, y, 0f);
        Vector3 finalPosition = spawnCenter.position + spawnOffset;

        // 3. Instantiate the enemy
        Instantiate(enemyPrefab, finalPosition, Quaternion.identity);
    }

    private void ResetTimer()
    {
        // Calculate dynamic rate: Delay decreases as score increases
        // Formula: Base - (Score * Scaling), clamped to MinRate
        float currentScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        float dynamicRate = baseSpawnRate - (currentScore * difficultyScaling);
        
        spawnTimer = Mathf.Max(dynamicRate, minSpawnRate);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnCenter != null)
        {
            Gizmos.color = Color.magenta;
            // Draw the perimeter circle for visualization
            DrawGizmoCircle(spawnCenter.position, spawnRadius);
        }
    }

    private void DrawGizmoCircle(Vector3 center, float radius)
    {
        float angleStep = 0.1f;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        for (float i = angleStep; i <= Mathf.PI * 2 + angleStep; i += angleStep)
        {
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(i) * radius, Mathf.Sin(i) * radius, 0);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}