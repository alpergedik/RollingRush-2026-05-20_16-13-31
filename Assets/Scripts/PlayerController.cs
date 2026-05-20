using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Movement")]
    public float laneDistance = 3f;
    public float laneChangeSpeed = 10f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float groundCheckDistance = 1.2f;
    public LayerMask groundLayer;

    [Header("Visual")]
    public Transform wheelVisual;
    public float visualRollSpeed = 8f;
    public float wheelRadius = 1f;

    private Rigidbody rb;
    private int currentLane = 0; // -1 = sol, 0 = orta, 1 = sağ
    private float startZ;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startZ = transform.position.z;
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

    HandleLaneInput();
    HandleJumpInput();
    RotateWheelVisual();
}

private void FixedUpdate()
{
    if (GameManager.Instance != null)
    {
        if (!GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)
        {
            return;
        }
    }

    MoveBetweenLanes();
}

    private void HandleLaneInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentLane > -1)
            {
                currentLane--;
            }
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentLane < 1)
            {
                currentLane++;
            }
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void MoveBetweenLanes()
    {
        Vector3 currentPosition = rb.position;

        float targetX = currentLane * laneDistance;

        float newX = Mathf.MoveTowards(
            currentPosition.x,
            targetX,
            laneChangeSpeed * Time.fixedDeltaTime
        );

        Vector3 targetPosition = new Vector3(
            newX,
            currentPosition.y,
            startZ
        );

        rb.MovePosition(targetPosition);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );
    }

private void RotateWheelVisual()
{
    if (wheelVisual == null)
    {
        return;
    }

    float speed = visualRollSpeed;

    if (GameManager.Instance != null)
    {
        speed = GameManager.Instance.currentSpeed;
    }

    float rotationAmount = (speed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;

    wheelVisual.Rotate(Vector3.right, -rotationAmount, Space.Self);
}

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
    private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Collectible"))
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectPart();
        }

        other.gameObject.SetActive(false);
    }
}
}