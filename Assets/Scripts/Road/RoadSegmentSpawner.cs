using System.Collections.Generic;
using UnityEngine;

public class RoadSegmentSpawner : MonoBehaviour
{
    [Header("Spawn Point Parents")]
    [SerializeField] private Transform obstaclePointsParent;
    [SerializeField] private Transform collectiblePointsParent;
    [SerializeField] private Transform spawnedObjectsParent;

    [Header("Obstacle Prefabs")]
    [SerializeField] private List<GameObject> obstaclePrefabs;

    [Header("Collectible Prefabs")]
    [SerializeField] private List<GameObject> collectiblePrefabs;

    [Header("Obstacle Spawn Settings")]
    [SerializeField] private bool spawnObstacle = true;
    [SerializeField] private int maxObstacleCount = 1;
    [Range(0f, 1f)]
    [SerializeField] private float obstacleSpawnChance = 0.75f;

    [Header("Collectible Spawn Settings")]
    [SerializeField] private bool spawnCollectibles = true;
    [SerializeField] private int minCollectibleCount = 1;
    [SerializeField] private int maxCollectibleCount = 2;
    [Range(0f, 1f)]
    [SerializeField] private float collectibleSpawnChance = 0.85f;

    private readonly List<Transform> obstaclePoints = new();
    private readonly List<Transform> collectiblePoints = new();
    private readonly List<GameObject> spawnedInstances = new();

    private void Awake()
    {
        EnsureSpawnedObjectsParent();
        CollectSpawnPoints();
    }

    private void Start()
    {
        // Initial spawn when the game starts
        RefreshSegmentSpawns();
    }

    private void EnsureSpawnedObjectsParent()
    {
        if (spawnedObjectsParent != null)
        {
            return;
        }

        Transform existing = transform.Find("SpawnedObjects");

        if (existing != null)
        {
            spawnedObjectsParent = existing;
            return;
        }

        GameObject container = new GameObject("SpawnedObjects");

        container.transform.SetParent(transform);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        spawnedObjectsParent = container.transform;
    }

    private void CollectSpawnPoints()
    {
        obstaclePoints.Clear();
        collectiblePoints.Clear();

        if (obstaclePointsParent != null)
        {
            foreach (Transform child in obstaclePointsParent)
            {
                if (child != null)
                {
                    obstaclePoints.Add(child);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[RoadSegmentSpawner] Obstacle Points Parent is missing on {gameObject.name}! Please assign it in Inspector.");
        }

        if (collectiblePointsParent != null)
        {
            foreach (Transform child in collectiblePointsParent)
            {
                if (child != null)
                {
                    collectiblePoints.Add(child);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[RoadSegmentSpawner] Collectible Points Parent is missing on {gameObject.name}! Please assign it in Inspector.");
        }
    }

    private void RemoveMissingSpawnPoints()
    {
        obstaclePoints.RemoveAll(point => point == null);
        collectiblePoints.RemoveAll(point => point == null);
    }

    public void RefreshSegmentSpawns()
    {
        RemoveMissingSpawnPoints();
        ClearSpawnedObjects();
        SpawnObstacles();
        SpawnCollectibles();
    }

    private void ClearSpawnedObjects()
    {
        for (int i = spawnedInstances.Count - 1; i >= 0; i--)
        {
            GameObject instance = spawnedInstances[i];

            if (instance != null)
            {
                Destroy(instance);
            }
        }

        spawnedInstances.Clear();
    }

    private GameObject SpawnPrefabAtPoint(GameObject prefab, Transform spawnPoint)
    {
        if (prefab == null || spawnPoint == null)
        {
            return null;
        }

        EnsureSpawnedObjectsParent();

        GameObject instance = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation,
            spawnedObjectsParent
        );

        spawnedInstances.Add(instance);

        return instance;
    }

    private void SpawnObstacles()
    {
        if (!spawnObstacle || obstaclePrefabs == null || obstaclePrefabs.Count == 0 || obstaclePoints.Count == 0)
        {
            return;
        }

        if (Random.value > obstacleSpawnChance)
        {
            return; // Failed chance
        }

        int countToSpawn = Mathf.Min(maxObstacleCount, obstaclePoints.Count);
        if (countToSpawn <= 0) return;

        List<Transform> availablePoints = new List<Transform>(obstaclePoints);
        
        for (int i = 0; i < countToSpawn; i++)
        {
            int pointIndex = Random.Range(0, availablePoints.Count);
            Transform spawnPoint = availablePoints[pointIndex];
            availablePoints.RemoveAt(pointIndex); // Prevent reusing the same point

            if (spawnPoint == null)
            {
                continue;
            }

            GameObject prefabToSpawn = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
            if (prefabToSpawn == null) continue;

            GameObject instance = SpawnPrefabAtPoint(prefabToSpawn, spawnPoint);

            if (instance != null)
            {
                if (instance.GetComponentInChildren<ObstacleMarker>() == null)
                {
                    Debug.LogWarning($"Spawned obstacle '{instance.name}' has no ObstacleMarker.", instance);
                }

                if (instance.GetComponentInChildren<Collider>() == null)
                {
                    Debug.LogWarning($"Spawned obstacle '{instance.name}' has no Collider.", instance);
                }
            }
        }
    }

    private void SpawnCollectibles()
    {
        if (!spawnCollectibles || collectiblePrefabs == null || collectiblePrefabs.Count == 0 || collectiblePoints.Count == 0)
        {
            return;
        }

        if (Random.value > collectibleSpawnChance)
        {
            return; // Failed chance
        }

        int maxPossible = Mathf.Min(maxCollectibleCount, collectiblePoints.Count);
        int countToSpawn = Random.Range(minCollectibleCount, maxPossible + 1);

        if (countToSpawn <= 0) return;

        List<Transform> availablePoints = new List<Transform>(collectiblePoints);

        for (int i = 0; i < countToSpawn; i++)
        {
            int pointIndex = Random.Range(0, availablePoints.Count);
            Transform spawnPoint = availablePoints[pointIndex];
            availablePoints.RemoveAt(pointIndex); // Prevent reusing the same point

            if (spawnPoint == null)
            {
                continue;
            }

            GameObject prefabToSpawn = collectiblePrefabs[Random.Range(0, collectiblePrefabs.Count)];
            if (prefabToSpawn == null) continue;

            SpawnPrefabAtPoint(prefabToSpawn, spawnPoint);
        }
    }
}
