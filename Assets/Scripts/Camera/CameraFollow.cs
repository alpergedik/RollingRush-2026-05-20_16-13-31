using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Spring Settings")]
    public float springStrength = 90f;
    public float damping = 6f;

    private Vector3 offset;
    private Vector3 velocity;

    void Start()
    {
        if (target == null) return;

        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;

        Vector3 displacement = targetPosition - transform.position;

        Vector3 springForce = displacement * springStrength;
        Vector3 dampingForce = -velocity * damping;

        Vector3 acceleration = springForce + dampingForce;

        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }
}