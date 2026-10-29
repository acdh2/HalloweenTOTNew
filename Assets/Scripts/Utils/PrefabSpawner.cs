using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] prefabsToSpawn;

    private Transform[] spawnPoints;

    void Awake()
    {
        InitializeSpawnPoints();
    }

    void Start()
    {
        SpawnPrefabs();
    }

    void InitializeSpawnPoints()
    {
        int count = transform.childCount;
        spawnPoints = new Transform[count];
        int index = 0;

        foreach (Transform child in transform)
        {
            if (child.name == "Spawn")
            {
                spawnPoints[index] = child;
                index++;
            }
        }

        // Resize array if fewer "Spawn" children were found
        if (index < count)
        {
            System.Array.Resize(ref spawnPoints, index);
        }
    }

    public void SpawnPrefabs()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        // Shuffle spawn points for random selection
        Transform[] shuffledPoints = (Transform[])spawnPoints.Clone();
        for (int i = 0; i < shuffledPoints.Length; i++)
        {
            Transform temp = shuffledPoints[i];
            int randomIndex = Random.Range(i, shuffledPoints.Length);
            shuffledPoints[i] = shuffledPoints[randomIndex];
            shuffledPoints[randomIndex] = temp;
        }

        int spawnCount = Mathf.Min(Random.Range(3, 8), shuffledPoints.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject randomPrefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];
            Transform spawnPoint = shuffledPoints[i];

            GameObject spawned = Instantiate(randomPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            spawned.name = randomPrefab.name;
        }
    }
}