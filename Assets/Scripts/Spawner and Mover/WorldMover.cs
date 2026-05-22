using UnityEngine;

public class WorldMover : MonoBehaviour
{
    [Header("Fallback Speed")]
    public float moveSpeed = 8f;

    [Header("Recycle Settings")]
    public bool recycle = false;
    public float recycleZ = -90f;
    public float segmentLength = 80f;
    public int segmentCount = 3;

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            return;
        }

        float speed = moveSpeed;

        if (GameManager.Instance != null)
        {
            speed = GameManager.Instance.currentSpeed;
        }

        transform.position += Vector3.back * speed * Time.fixedDeltaTime;

        if (recycle && transform.position.z <= recycleZ)
        {
            float newZ = transform.position.z + segmentLength * segmentCount;
            transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
        }
    }
}