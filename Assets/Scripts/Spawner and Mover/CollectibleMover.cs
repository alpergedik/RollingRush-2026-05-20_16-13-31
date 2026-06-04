using UnityEngine;

public class CollectibleMover : MonoBehaviour
{
    public float disableZ = -40f;
    public float rotateSpeed = 180f;

    private void FixedUpdate()
    {
        // Movement is now handled by RoadLooper moving the entire RoadSegment
        // The disableZ logic is also handled by RoadSegmentSpawner.ClearSpawnedObjects()
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            return;
        }

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
}