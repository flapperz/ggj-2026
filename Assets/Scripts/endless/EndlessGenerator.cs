using System.Collections.Generic;
using UnityEngine;

public class EndlessGenerator : MonoBehaviour
{
    [Header("References")]
    public GameObject startingPrefab; // The specific prefab for the beginning
    public List<GameObject> templates;

    [Header("Movement Settings")]
    public float scrollSpeed = 10f;
    public float templateWidth = 50f;
    public int initialSpawnCount = 3;

    private float nextSpawnX = 0f;
    private List<GameObject> activeTemplates = new List<GameObject>();
    private int spawnedCount = 0; // Tracks total spawned to handle logic

    void Start()
    {
        // Initial setup
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnTemplate();
        }
    }

    void Update()
    {
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        if (transform.position.x + nextSpawnX < (initialSpawnCount * templateWidth))
        {
            SpawnTemplate();
            CleanupOldTemplates();
        }
    }

    private void SpawnTemplate()
    {
        GameObject prefabToSpawn;

        // Logic: Use startingPrefab for the first 2 spawns, then go random
        if (spawnedCount < 2 && startingPrefab != null)
        {
            prefabToSpawn = startingPrefab;
        }
        else
        {
            if (templates == null || templates.Count == 0) return;
            prefabToSpawn = templates[Random.Range(0, templates.Count)];
        }

        Vector3 localPos = new Vector3(nextSpawnX, 0, 0);
        GameObject newTemplate = Instantiate(prefabToSpawn, transform);
        newTemplate.transform.localPosition = localPos;

        activeTemplates.Add(newTemplate);

        nextSpawnX += templateWidth;
        spawnedCount++; // Increment the counter
    }

    private void CleanupOldTemplates()
    {
        if (activeTemplates.Count > 0)
        {
            if (activeTemplates[0].transform.position.x < -templateWidth * 1.5f)
            {
                GameObject oldTemplate = activeTemplates[0];
                activeTemplates.RemoveAt(0);
                Destroy(oldTemplate);
            }
        }
    }
}