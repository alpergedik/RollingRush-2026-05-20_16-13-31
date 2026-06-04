using UnityEngine;

public class CollectibleMover : MonoBehaviour
{
    public float disableZ = -40f;
    public float rotateSpeed = 180f;

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            return;
        }

        float speed = 8f;

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

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            return;
        }

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
}