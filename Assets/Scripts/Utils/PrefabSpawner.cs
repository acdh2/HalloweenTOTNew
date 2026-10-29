using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] prefabsToSpawn;

    [Header("Count")]
    public int minCount = 3;
    public int maxCount = 7;

    [Header("Layout")]
    public float spacing = 2.0f;

    void Start()
    {
        SpawnPrefabs();
    }

    public void SpawnPrefabs()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0) return;

        int count = Random.Range(minCount, maxCount + 1);
        float totalWidth = (count - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            GameObject randomPrefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];
            
            Vector3 spawnPosition = transform.position + new Vector3(startX + (i * spacing), 0f, 0f);
            
            GameObject spawned = Instantiate(randomPrefab, spawnPosition, transform.rotation, transform);
            spawned.name = randomPrefab.name;
        }
    }
}