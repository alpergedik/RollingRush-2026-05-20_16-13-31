using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [SerializeField] private float minX = -4.5f;
    [SerializeField] private float maxX = 4.5f;
    [SerializeField] private float maxHorizontalSpeed = 7f;
    [SerializeField] private float horizontalAcceleration = 20f;
    [SerializeField] private float horizontalDeceleration = 14f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckDistance = 1.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual Roll")]
    [SerializeField] private Transform rollPivot;
    [SerializeField] private float visualRollSpeed = 8f;
    [SerializeField] private float wheelRadius = 1f;

    [Header("Visual Lean (Yalpalama)")]
    [SerializeField] private Transform leanPivot;
    [SerializeField] private float maxLeanAngle = 12f;
    [SerializeField] private float maxSteerAngle = 10f;
    [SerializeField] private float leanSmooth = 8f;

    [Header("Natural Wobble (Sallanma)")]
    [SerializeField] private float idleWobbleLeanAmount = 3f;
    [SerializeField] private float idleWobbleSteerAmount = 1.5f;
    [SerializeField] private float idleWobbleSpeed = 2.1f;
    [SerializeField] private float wobbleInputFade = 0.25f;

    [SerializeField] private float referenceSpeed = 10f;
    [SerializeField] private float minWobbleSpeedMultiplier = 0.45f;
    [SerializeField] private float maxWobbleSpeedMultiplier = 1.8f;
    [SerializeField] private float highSpeedWobbleAmountMultiplier = 1.25f;
    [SerializeField] private float lowSpeedWobbleAmountMultiplier = 0.55f;

    [Header("Drift")]
    [SerializeField] private bool enableDrift = true;

    [Tooltip("Bu hızın altında drift başlamaz.")]
    [SerializeField] private float minDriftForwardSpeed = 11f;

    [Tooltip("Bu hızda drift intensity maksimuma yaklaşır.")]
    [SerializeField] private float fullDriftForwardSpeed = 16f;

    [Tooltip("Yatay hız bu değerin altındaysa drift başlamaz.")]
    [SerializeField] private float minDriftHorizontalSpeed = 2.5f;

    [Tooltip("Yatay hız bu değere yaklaşınca drift intensity güçlenir.")]
    [SerializeField] private float fullDriftHorizontalSpeed = 7f;

    [SerializeField] private float driftBuildSpeed = 5f;
    [SerializeField] private float driftFadeSpeed = 4f;

    [Tooltip("Drift sırasında tekerin yan yüzeyini göstermek için Y eksenindeki ekstra dönüş.")]
    [SerializeField] private float driftSideViewAngle = 28f;

    [Tooltip("Drift sırasında ekstra yana yatma.")]
    [SerializeField] private float driftExtraLeanAngle = 8f;

    [Header("Drift Score")]
    [SerializeField] private float driftScorePerSecond = 25f;
    [SerializeField] private float driftScoreTickInterval = 0.1f;

    [Header("Drift FX")]
    [SerializeField] private ParticleSystem driftDustParticle;
    [SerializeField] private float maxDriftDustEmission = 45f;

    [SerializeField] private float driftDustBackwardVelocity = 3.5f;
    [SerializeField] private float driftDustSideVelocity = 0.8f;
    [SerializeField] private float driftDustUpVelocity = 0.2f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private float startZ;
    private float horizontalInput;
    private float currentHorizontalVelocity;
    private float wobbleTimer;
    private bool isGrounded;

    private float driftIntensity;
    private float driftScoreTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startZ = transform.position.z;

        SetDriftDustEmission(0f);

        if (driftDustParticle != null)
        {
            driftDustParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        if (!CanUpdatePlayer())
        {
            return;
        }

        ReadHorizontalInput();
        UpdateGroundedState();
        HandleJumpInput();
        RotateWheelVisual();
        UpdateDriftState();
        UpdateLeanVisual();
        UpdateDriftScore();
        UpdateDriftFX();
    }


    private void FixedUpdate()
    {
        if (!CanUpdatePlayer())
        {
            return;
        }

        MoveHorizontally();
    }

    private bool CanUpdatePlayer()
    {
        if (GameManager.Instance == null)
        {
            return true;
        }

        return GameManager.Instance.isGameStarted && !GameManager.Instance.isGameOver;
    }

    private void ReadHorizontalInput()
    {
        horizontalInput = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput += 1f;
        }

        horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
    }

    private void MoveHorizontally()
    {
        float targetVelocity = horizontalInput * maxHorizontalSpeed;

        float speedChangeRate = Mathf.Abs(horizontalInput) > 0.01f
            ? horizontalAcceleration
            : horizontalDeceleration;

        currentHorizontalVelocity = Mathf.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            speedChangeRate * Time.fixedDeltaTime
        );

        Vector3 currentPosition = rb.position;

        float newX = currentPosition.x + currentHorizontalVelocity * Time.fixedDeltaTime;
        newX = Mathf.Clamp(newX, minX, maxX);

        if ((newX <= minX && currentHorizontalVelocity < 0f) ||
            (newX >= maxX && currentHorizontalVelocity > 0f))
        {
            currentHorizontalVelocity = 0f;
        }

        Vector3 targetPosition = new Vector3(
            newX,
            currentPosition.y,
            startZ
        );

        rb.MovePosition(targetPosition);
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    
    private void UpdateGroundedState()
    {
        isGrounded = IsGrounded();
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
        if (rollPivot == null)
        {
            return;
        }

        float speed = visualRollSpeed;

        if (GameManager.Instance != null)
        {
            speed = GameManager.Instance.currentSpeed;
        }

        float rotationAmount = (speed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;

        rollPivot.Rotate(Vector3.forward, rotationAmount, Space.Self);
    }

    private void UpdateDriftState()
    {
        if (!enableDrift)
        {
            driftIntensity = Mathf.MoveTowards(
                driftIntensity,
                0f,
                driftFadeSpeed * Time.deltaTime
            );

            return;
        }

        float forwardSpeed = GetCurrentForwardSpeed();
        float lateralSpeed = Mathf.Abs(currentHorizontalVelocity);
        float inputStrength = Mathf.Abs(horizontalInput);

        bool hasForwardSpeed = forwardSpeed >= minDriftForwardSpeed;
        bool hasLateralMovement = lateralSpeed >= minDriftHorizontalSpeed;
        bool hasSteerInput = inputStrength > 0.1f;

        float targetDriftIntensity = 0f;

        if (isGrounded && hasForwardSpeed && hasLateralMovement && hasSteerInput)        {
            float forwardFactor = Mathf.InverseLerp(
                minDriftForwardSpeed,
                fullDriftForwardSpeed,
                forwardSpeed
            );

            float lateralFactor = Mathf.InverseLerp(
                minDriftHorizontalSpeed,
                fullDriftHorizontalSpeed,
                lateralSpeed
            );

            targetDriftIntensity = Mathf.Clamp01(forwardFactor * lateralFactor);
        }

        float changeSpeed = targetDriftIntensity > driftIntensity
            ? driftBuildSpeed
            : driftFadeSpeed;

        driftIntensity = Mathf.MoveTowards(
            driftIntensity,
            targetDriftIntensity,
            changeSpeed * Time.deltaTime
        );
    }

    private void UpdateLeanVisual()
    {
        if (leanPivot == null)
        {
            return;
        }

        float velocityPercent = 0f;

        if (maxHorizontalSpeed > 0f)
        {
            velocityPercent = currentHorizontalVelocity / maxHorizontalSpeed;
        }

        float inputStrength = Mathf.Abs(horizontalInput);

        float currentForwardSpeed = GetCurrentForwardSpeed();

        float speedRatio = 1f;

        if (referenceSpeed > 0f)
        {
            speedRatio = currentForwardSpeed / referenceSpeed;
        }

        float wobbleSpeedMultiplier = Mathf.Clamp(
            speedRatio,
            minWobbleSpeedMultiplier,
            maxWobbleSpeedMultiplier
        );

        float wobbleAmountMultiplier = Mathf.Lerp(
            lowSpeedWobbleAmountMultiplier,
            highSpeedWobbleAmountMultiplier,
            Mathf.InverseLerp(0.5f, 1.6f, speedRatio)
        );

        wobbleTimer += Time.deltaTime * idleWobbleSpeed * wobbleSpeedMultiplier;

        float targetLeanZ = -velocityPercent * maxLeanAngle;
        float targetSteerY = velocityPercent * maxSteerAngle;

        float driftDirection = GetDriftDirection();

        targetSteerY += driftDirection * driftSideViewAngle * driftIntensity;
        targetLeanZ += -driftDirection * driftExtraLeanAngle * driftIntensity;

        float wobbleFade = Mathf.Lerp(1f, wobbleInputFade, inputStrength);

        float wobbleLeanZ =
            Mathf.Sin(wobbleTimer) * idleWobbleLeanAmount * wobbleAmountMultiplier;

        float wobbleSteerY =
            Mathf.Sin(wobbleTimer * 0.67f + 1.3f) * idleWobbleSteerAmount * wobbleAmountMultiplier;

        targetLeanZ += wobbleLeanZ * wobbleFade;
        targetSteerY += wobbleSteerY * wobbleFade;

        Quaternion targetRotation = Quaternion.Euler(
            0f,
            targetSteerY,
            targetLeanZ
        );

        leanPivot.localRotation = Quaternion.Slerp(
            leanPivot.localRotation,
            targetRotation,
            leanSmooth * Time.deltaTime
        );
    }

    private void UpdateDriftScore()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!isGrounded || driftIntensity < 0.15f)
        {
            driftScoreTimer = 0f;
            return;
        }

        driftScoreTimer += Time.deltaTime;

        if (driftScoreTimer < driftScoreTickInterval)
        {
            return;
        }

        int scoreAmount = Mathf.Max(
            1,
            Mathf.RoundToInt(driftScorePerSecond * driftIntensity * driftScoreTickInterval)
        );

        GameManager.Instance.AddDriftScore(scoreAmount);

        driftScoreTimer = 0f;
    }

    private void UpdateDriftFX()
    {
        if (driftDustParticle == null)
        {
            return;
        }

        if (isGrounded && driftIntensity > 0.1f)
        {
            if (!driftDustParticle.isPlaying)
            {
                driftDustParticle.Play();
            }

            SetDriftDustEmission(maxDriftDustEmission * driftIntensity);
            UpdateDriftDustVelocity();
        }
        else
        {
            SetDriftDustEmission(0f);

            if (driftDustParticle.isPlaying)
            {
                driftDustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
    
    private void UpdateDriftDustVelocity()
    {
        if (driftDustParticle == null)
        {
            return;
        }

        Transform cam = cameraTransform;

        if (cam == null && Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        if (cam == null)
        {
            return;
        }

        float driftDirection = GetDriftDirection();

        Vector3 cameraForward = cam.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cam.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        // Kameraya göre ekranda geriye doğru akış.
        Vector3 backwardDirection = -cameraForward;

        // Drift yönünün tersine hafif çapraz dağılım.
        Vector3 sideDirection = -cameraRight * driftDirection;

        Vector3 finalVelocity =
            backwardDirection * driftDustBackwardVelocity +
            sideDirection * driftDustSideVelocity +
            Vector3.up * driftDustUpVelocity;

        finalVelocity *= driftIntensity;

        ParticleSystem.VelocityOverLifetimeModule velocity = driftDustParticle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        velocity.x = new ParticleSystem.MinMaxCurve(finalVelocity.x);
        velocity.y = new ParticleSystem.MinMaxCurve(finalVelocity.y);
        velocity.z = new ParticleSystem.MinMaxCurve(finalVelocity.z);
    }

    private void SetDriftDustEmission(float rate)
    {
        if (driftDustParticle == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = driftDustParticle.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
    }

    private float GetCurrentForwardSpeed()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.currentSpeed;
        }

        return referenceSpeed;
    }

    private float GetDriftDirection()
    {
        if (Mathf.Abs(currentHorizontalVelocity) > 0.05f)
        {
            return Mathf.Sign(currentHorizontalVelocity);
        }

        if (Mathf.Abs(horizontalInput) > 0.05f)
        {
            return Mathf.Sign(horizontalInput);
        }

        return 0f;
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