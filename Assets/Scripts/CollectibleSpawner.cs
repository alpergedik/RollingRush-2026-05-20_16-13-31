using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject collectiblePrefab;

    [Header("Pool")]
    public int poolSize = 20;

    [Header("Spawn Settings")]
    public float spawnZ = 65f;
    public float spawnY = 1f;
    public float minSpawnX = -4.2f;
    public float maxSpawnX = 4.2f;

    [Header("Timing")]
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 1.2f;

    private readonly List<GameObject> collectiblePool = new List<GameObject>();
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
            SpawnCollectible();
            spawnTimer = 0f;
            SetRandomSpawnInterval();
        }
    }

    private void CreatePool()
    {
        if (collectiblePrefab == null)
        {
            Debug.LogWarning("CollectibleSpawner: Collectible prefab is missing.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject collectible = Instantiate(collectiblePrefab, transform);
            collectible.SetActive(false);
            collectiblePool.Add(collectible);
        }
    }

    private void SpawnCollectible()
    {
        GameObject collectible = GetInactiveCollectible();

        if (collectible == null)
        {
            return;
        }

        float xPosition = Random.Range(minSpawnX, maxSpawnX);

        collectible.transform.position = new Vector3(xPosition, spawnY, spawnZ);
        collectible.transform.rotation = Quaternion.identity;

        collectible.SetActive(true);
    }

    private GameObject GetInactiveCollectible()
    {
        foreach (GameObject collectible in collectiblePool)
        {
            if (!collectible.activeInHierarchy)
            {
                return collectible;
            }
        }

        return null;
    }

    private void SetRandomSpawnInterval()
    {
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}