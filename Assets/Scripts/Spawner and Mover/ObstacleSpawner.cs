using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] obstaclePrefabs;

    [Header("Pool")]
    public int poolSizePerPrefab = 5;

    [Header("Spawn Settings")]
    public float spawnZ = 65f;
    public float spawnY = 0f;
    public float minSpawnX = -4.2f;
    public float maxSpawnX = 4.2f;

    [Header("Timing")]
    public float minSpawnInterval = 1.0f;
    public float maxSpawnInterval = 2.0f;

    private readonly List<GameObject> obstaclePool = new List<GameObject>();
    private float spawnTimer;
    private float currentSpawnInterval;

    private void Start()
    {
        CreatePool();
        SetRandomSpawnInterval();
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)
            {
                return;
            }
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentSpawnInterval)
        {
            SpawnObstacle();
            spawnTimer = 0f;
            SetRandomSpawnInterval();
        }
    }

    private void CreatePool()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogWarning("ObstacleSpawner: Obstacle prefabs are missing.");
            return;
        }

        for (int prefabIndex = 0; prefabIndex < obstaclePrefabs.Length; prefabIndex++)
        {
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject obstacle = Instantiate(obstaclePrefabs[prefabIndex], transform);
                obstacle.SetActive(false);
                obstaclePool.Add(obstacle);
            }
        }
    }

    private void SpawnObstacle()
    {
        GameObject obstacle = GetRandomInactiveObstacle();

        if (obstacle == null)
        {
            return;
        }

        float xPosition = Random.Range(minSpawnX, maxSpawnX);

        obstacle.transform.position = new Vector3(xPosition, spawnY, spawnZ);
        obstacle.transform.rotation = Quaternion.identity;
        
        obstacle.SetActive(true);
    }

    private GameObject GetRandomInactiveObstacle()
    {
        List<GameObject> inactiveObstacles = new List<GameObject>();

        foreach (GameObject obstacle in obstaclePool)
        {
            if (!obstacle.activeInHierarchy)
            {
                inactiveObstacles.Add(obstacle);
            }
        }

        if (inactiveObstacles.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, inactiveObstacles.Count);
        return inactiveObstacles[randomIndex];
    }

    private void SetRandomSpawnInterval()
    {
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}