using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float disableZ = -40f;

    private void FixedUpdate()
    {
        // Movement is now handled by RoadLooper moving the entire RoadSegment
        // The disableZ logic is also handled by RoadSegmentSpawner.ClearSpawnedObjects()
    }
}