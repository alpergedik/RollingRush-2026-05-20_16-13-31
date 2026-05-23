using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float followSpeed = 5f;

    private Vector3 offset;
    private float fixedYPosition;

    private void Start()
    {
        if (target == null)
        {
            return;
        }

        offset = transform.position - target.position;

        fixedYPosition = transform.position.y;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = new Vector3(
            target.position.x + offset.x,
            fixedYPosition,
            target.position.z + offset.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }
}