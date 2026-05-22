using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float disableZ = -20f;

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

        if (transform.position.z <= disableZ)
        {
            gameObject.SetActive(false);
        }
    }
}