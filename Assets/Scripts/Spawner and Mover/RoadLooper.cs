using UnityEngine;

public class RoadLooper : MonoBehaviour
{
    [Header("Road Segments")]
    public Transform[] roadSegments;

    [Header("Settings")]
    public float segmentLength = 80f;
    public float playerZ = -10f;
    public float safeBuffer = 5f;

    [Header("Fallback")]
    public float fallbackSpeed = 8f;

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            return;
        }

        float speed = fallbackSpeed;

        if (GameManager.Instance != null)
        {
            speed = GameManager.Instance.currentSpeed;
        }

        MoveRoads(speed);
        RecycleRoads();
    }

    private void MoveRoads(float speed)
    {
        for (int i = 0; i < roadSegments.Length; i++)
        {
            roadSegments[i].position += Vector3.back * speed * Time.fixedDeltaTime;
        }
    }

    private void RecycleRoads()
    {
        for (int i = 0; i < roadSegments.Length; i++)
        {
            Transform road = roadSegments[i];

            float roadFrontEdgeZ = road.position.z + segmentLength / 2f;

            if (roadFrontEdgeZ < playerZ - safeBuffer)
            {
                float furthestZ = GetFurthestRoadZ();

                road.position = new Vector3(
                    road.position.x,
                    road.position.y,
                    furthestZ + segmentLength
                );
            }
        }
    }

    private float GetFurthestRoadZ()
    {
        float furthestZ = roadSegments[0].position.z;

        for (int i = 1; i < roadSegments.Length; i++)
        {
            if (roadSegments[i].position.z > furthestZ)
            {
                furthestZ = roadSegments[i].position.z;
            }
        }

        return furthestZ;
    }
}