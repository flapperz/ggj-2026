using System.Collections.Generic;
using UnityEngine;

public class EndlessGenerator : MonoBehaviour
{
    [Header("References")]
    public List<GameObject> templates;

    [Header("Movement Settings")]
    public float scrollSpeed = 10f; // Speed at which the world moves left
    public float templateWidth = 50f;
    public int initialSpawnCount = 3;

    private float nextSpawnX = 0f;
    private List<GameObject> activeTemplates = new List<GameObject>();

    void Start()
    {
        // Initial setup: Spawn pieces starting from the center and moving right
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnTemplate();
        }
    }

    void Update()
    {
        // 1. Move the entire generator (and all child templates) to the left
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        // 2. Check if the "end" of our generated track has entered the view
        // Since the generator is moving left, nextSpawnX (in local space) 
        // will eventually cross back into world positive space.
        
        // We want to keep at least 'initialSpawnCount' ahead of world zero
        if (transform.position.x + nextSpawnX < (initialSpawnCount * templateWidth))
        {
            SpawnTemplate();
            CleanupOldTemplates();
        }
    }

    private void SpawnTemplate()
    {
        if (templates == null || templates.Count == 0) return;

        GameObject prefab = templates[Random.Range(0, templates.Count)];
        
        // Position is relative to this Transform (the Generator)
        Vector3 localPos = new Vector3(nextSpawnX, 0, 0);
        
        // Instantiate as a child so it moves with the Generator
        GameObject newTemplate = Instantiate(prefab, transform);
        newTemplate.transform.localPosition = localPos;

        activeTemplates.Add(newTemplate);

        // Advance the local pointer
        nextSpawnX += templateWidth;
    }

    private void CleanupOldTemplates()
    {
        // If the oldest template's world position is far behind the player (e.g., -60)
        if (activeTemplates.Count > 0)
        {
            // Check the first template in the list
            if (activeTemplates[0].transform.position.x < -templateWidth * 1.5f)
            {
                GameObject oldTemplate = activeTemplates[0];
                activeTemplates.RemoveAt(0);
                Destroy(oldTemplate);
            }
        }
    }
}