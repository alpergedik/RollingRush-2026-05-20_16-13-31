using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Game Balance Cache")]
    private BalanceSettings balance;

    [Header("Wheel Settings")]
    [Tooltip("Teker yarıçapı, dönüş hızını hesaplamak için kullanılır.")]
    [SerializeField] private float wheelRadius = 1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("SphereCast genişliği. Zemin algısı kopuyorsa artırılabilir.")]
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private bool debugGroundCheck = false;

    [Header("Wheel Visuals")]
    [SerializeField] private Transform rollPivot;
    [SerializeField] private Transform leanPivot;
    [Tooltip("Tekerin sağ-sol yatma açısı.")]
    [SerializeField] private float maxLeanAngle = 12f;
    [Tooltip("Tekerin sağ-sol yönelme açısı.")]
    [SerializeField] private float maxSteerAngle = 10f;

    [Header("Road Feel")]
    [Tooltip("Input yokken bozuk yolun tekeri sağa/sola çekme gücü.")]
    [SerializeField] private float roadDriftStrength = 0.8f;
    [Tooltip("Bozuk yol yön değişiminin ne kadar hızlı değişeceği.")]
    [SerializeField] private float roadDriftChangeSpeed = 0.35f;

    [Header("Drift FX")]
    [SerializeField] private float driftFxThreshold = 0.1f;
    [SerializeField] private ParticleSystem driftDustParticle;
    [SerializeField] private float maxDriftDustEmission = 45f;
    [SerializeField] private Transform cameraTransform;

    [Header("Stone FX")]
    [SerializeField] private ParticleSystem stoneParticle;

    [Header("Crash Animation")]
    [SerializeField] private float crashAnimationDuration = 1.15f;
    [SerializeField] private float crashKnockbackZ = 1.2f;
    [SerializeField] private float crashSpinAngle = 520f;
    [SerializeField] private float crashLeanAngle = 85f;

    [Header("Audio Settings")]
    [SerializeField] private float boundaryHitSoundCooldown = 0.35f;
    private float lastBoundaryHitSoundTime = -999f;

    // --- Hidden Settings (Still active in code) ---
    private float leanSmooth = 10f;
    
    private float idleWobbleLeanAmount = 3f;
    private float idleWobbleSteerAmount = 1.5f;
    private float idleWobbleSpeed = 2.4f;
    private float wobbleInputFade = 0.35f;
    private float referenceSpeed = 10f;
    private float minWobbleSpeedMultiplier = 0.45f;
    private float maxWobbleSpeedMultiplier = 1.8f;
    private float highSpeedWobbleAmountMultiplier = 1.25f;
    private float lowSpeedWobbleAmountMultiplier = 0.55f;

    [SerializeField] private float driftScoreThreshold = 0.15f;
    private float driftScorePerSecond = 40f;
    private float driftScoreTickInterval = 0.1f;

    private float driftDustBackwardVelocity = 15f;
    private float driftDustSideVelocity = 5f;
    private float driftDustUpVelocity = 2.5f;

    private float stoneMinSpeed = 2f;
    private float stoneFullSpeed = 10f;
    private float stoneMaxEmission = 30f;
    private float stoneDriftSuppressThreshold = 0.25f;

    private float crashSideForce = 0.65f;
    private float crashUpForce = 0.45f;
    private float crashFallDownY = 0.55f;
    private Ease crashImpactEase = Ease.OutQuad;
    private Ease crashFallEase = Ease.InQuad;

    private float jumpGroundLockTime = 0.2f;
    private bool hasJumped;
    private float jumpLockTimer;

    private Rigidbody rb;
    private float startZ;
    private float horizontalInput;
    private float currentHorizontalVelocity;
    private float roadDriftSeed;
    private float currentRoadDrift;
    private float wobbleTimer;
    private bool isGrounded;
    private bool isCrashing;
    
    private float driftIntensity;
    private float driftScoreTimer;

    // Public Read-Only Properties
    public bool IsGrounded => isGrounded;
    public bool IsCrashing => isCrashing;
    public float DriftIntensity => driftIntensity;
    public float CurrentHorizontalVelocity => currentHorizontalVelocity;
    public float CurrentForwardSpeed => GetCurrentForwardSpeed();

    public bool IsDriftingForFX =>
        isGrounded &&
        !isCrashing &&
        driftIntensity > driftFxThreshold;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startZ = transform.position.z;
    
        roadDriftSeed = Random.Range(0f, 1000f);

        SetDriftDustEmission(0f);

        if (driftDustParticle != null)
        {
            driftDustParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        SetStoneEmission(0f);

        if (stoneParticle != null)
        {
            stoneParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDisable()
    {
        transform.DOKill();

        if (leanPivot != null)
        {
            leanPivot.DOKill();
        }

        if (rollPivot != null)
        {
            rollPivot.DOKill();
        }
    }

    private void Update()
    {
        if (balance == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.balanceConfig != null)
            {
                balance = GameManager.Instance.Balance;
            }
            else
            {
                balance = new BalanceSettings(); // Fallback
            }
        }

        if (!CanUpdatePlayer())
        {
            SoundManager.Instance?.SetRollingIntensity(0f);
            SoundManager.Instance?.SetDriftIntensity(0f);
            return;
        }

        ReadHorizontalInput();
        UpdateGroundedState();

        if (jumpLockTimer > 0f)
        {
            jumpLockTimer -= Time.deltaTime;
        }

        if (isGrounded && jumpLockTimer <= 0f && rb.linearVelocity.y <= 0.1f)
        {
            hasJumped = false;
        }

        HandleJumpInput();
        RotateWheelVisual();
        UpdateDriftState();
        UpdateLeanVisual();
        UpdateDriftScore();
        UpdateDriftFX();
        UpdateStoneFX();
        UpdateGameplayAudio();
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
        if (isCrashing)
        {
            return false;
        }

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
        float targetVelocity = horizontalInput * balance.maxHorizontalSpeed;

        float roadDrift = 0f;

        if (isGrounded && Mathf.Abs(horizontalInput) < 0.01f)
        {
            float noise = Mathf.PerlinNoise(
                Time.time * roadDriftChangeSpeed,
                roadDriftSeed
            );

            roadDrift = (noise - 0.5f) * 2f * roadDriftStrength;

            targetVelocity += roadDrift;
            targetVelocity = Mathf.Clamp(targetVelocity, -balance.maxHorizontalSpeed, balance.maxHorizontalSpeed);
        }

        currentRoadDrift = roadDrift;

        float speedChangeRate = Mathf.Abs(horizontalInput) > 0.01f
            ? balance.horizontalAcceleration
            : balance.horizontalDeceleration;

        currentHorizontalVelocity = Mathf.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            speedChangeRate * Time.fixedDeltaTime
        );

        Vector3 currentPosition = rb.position;

        float newX = currentPosition.x + currentHorizontalVelocity * Time.fixedDeltaTime;

        Vector3 targetPosition = new Vector3(
            newX,
            currentPosition.y,
            startZ
        );

        rb.MovePosition(targetPosition);
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !hasJumped && jumpLockTimer <= 0f)
        {
            hasJumped = true;
            jumpLockTimer = jumpGroundLockTime;
            
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * balance.targetJumpHeight);
            rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
            
            SoundManager.Instance?.PlayJump();
        }
    }
    
    private void UpdateGroundedState()
    {
        bool wasGrounded = isGrounded;
        isGrounded = CheckIsGrounded();
        
        if (!wasGrounded && isGrounded && GameManager.Instance != null && GameManager.Instance.isGameStarted)
        {
            SoundManager.Instance?.PlayLanding();
        }
    }

    private bool CheckIsGrounded()
    {
        Vector3 origin = groundCheckPoint != null
            ? groundCheckPoint.position
            : transform.position + Vector3.up * 0.4f;

        bool hit = Physics.CheckSphere(
            origin,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (debugGroundCheck)
        {
            Debug.DrawRay(
                origin,
                hit ? Vector3.up * 0.5f : Vector3.down * 0.5f,
                hit ? Color.green : Color.red
            );
        }

        return hit;
    }

    private void RotateWheelVisual()
    {
        if (rollPivot == null)
        {
            return;
        }

        float speed = GameManager.Instance != null ? GameManager.Instance.currentSpeed : referenceSpeed;

        float rotationAmount = (speed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;

        rollPivot.Rotate(Vector3.forward, rotationAmount, Space.Self);
    }

    private void UpdateDriftState()
    {
        float forwardSpeed = GetCurrentForwardSpeed();
        float lateralSpeed = Mathf.Abs(currentHorizontalVelocity);
        float inputStrength = Mathf.Abs(horizontalInput);

        bool hasForwardSpeed = forwardSpeed >= balance.minDriftForwardSpeed;
        bool hasLateralMovement = lateralSpeed >= balance.minDriftHorizontalSpeed;
        bool hasSteerInput = inputStrength > 0.1f;

        float targetDriftIntensity = 0f;

        if (isGrounded && hasForwardSpeed && hasLateralMovement && hasSteerInput)
        {
            float forwardFactor = Mathf.InverseLerp(
                balance.minDriftForwardSpeed,
                balance.fullDriftForwardSpeed,
                forwardSpeed
            );

            float lateralFactor = Mathf.InverseLerp(
                balance.minDriftHorizontalSpeed,
                balance.fullDriftHorizontalSpeed,
                lateralSpeed
            );

            targetDriftIntensity = Mathf.Clamp01(forwardFactor * lateralFactor);
        }

        float changeSpeed = targetDriftIntensity > driftIntensity
            ? balance.driftBuildSpeed
            : balance.driftFadeSpeed;

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

        if (balance.maxHorizontalSpeed > 0f)
        {
            velocityPercent = currentHorizontalVelocity / balance.maxHorizontalSpeed;
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

        float roadDriftPercent = 0f;

        if (isGrounded && Mathf.Abs(horizontalInput) < 0.01f && balance.maxHorizontalSpeed > 0f)
        {
            roadDriftPercent = currentRoadDrift / balance.maxHorizontalSpeed;
        }

        float visualDirectionPercent = Mathf.Abs(horizontalInput) > 0.01f
            ? velocityPercent
            : roadDriftPercent;

        visualDirectionPercent = Mathf.Clamp(visualDirectionPercent, -1f, 1f);

        float targetLeanZ = -visualDirectionPercent * maxLeanAngle;
        float targetSteerY = visualDirectionPercent * maxSteerAngle;

        float driftDirection = GetDriftDirection();

        targetSteerY += driftDirection * balance.driftSideViewAngle * driftIntensity;
        targetLeanZ += -driftDirection * balance.driftExtraLeanAngle * driftIntensity;

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

        if (!isGrounded || driftIntensity < driftScoreThreshold)
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

        if (isGrounded && driftIntensity > driftFxThreshold)
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

    private void UpdateStoneFX()
    {
        if (stoneParticle == null)
        {
            return;
        }

        float currentSpeed = GetCurrentForwardSpeed();

        bool shouldEmit =
            isGrounded &&
            !isCrashing &&
            currentSpeed > stoneMinSpeed;

        if (!shouldEmit)
        {
            SetStoneEmission(0f);

            if (stoneParticle.isPlaying)
            {
                stoneParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            return;
        }

        float speedFactor = Mathf.InverseLerp(
            stoneMinSpeed,
            stoneFullSpeed,
            currentSpeed
        );

        float driftSuppression = Mathf.InverseLerp(
            stoneDriftSuppressThreshold,
            1f,
            driftIntensity
        );

        float emissionRate =
            stoneMaxEmission *
            speedFactor *
            (1f - driftSuppression);

        emissionRate = Mathf.Max(0f, emissionRate);

        if (emissionRate > 0.1f)
        {
            if (!stoneParticle.isPlaying)
            {
                stoneParticle.Play();
            }

            SetStoneEmission(emissionRate);
        }
        else
        {
            SetStoneEmission(0f);

            if (stoneParticle.isPlaying)
            {
                stoneParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private void SetStoneEmission(float rate)
    {
        if (stoneParticle == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = stoneParticle.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
    }

    private void UpdateGameplayAudio()
    {
        SoundManager.Instance?.SetDriftIntensity(isGrounded ? driftIntensity : 0f);

        float speed = GetCurrentForwardSpeed();
        float rollingIntensity = Mathf.InverseLerp(2f, 10f, speed);
        rollingIntensity *= 1f - Mathf.Clamp01(driftIntensity * 0.6f);
        SoundManager.Instance?.SetRollingIntensity(isGrounded ? rollingIntensity : 0f);
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
        ObstacleMarker obstacle = collision.collider.GetComponentInParent<ObstacleMarker>();

        if (obstacle != null)
        {
            TriggerGameOverFromObstacle(collision);
            return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("RoadBoundary"))
        {
            currentHorizontalVelocity = 0f;
            if (Time.time - lastBoundaryHitSoundTime >= boundaryHitSoundCooldown)
            {
                SoundManager.Instance?.PlayBoundaryHit();
                lastBoundaryHitSoundTime = Time.time;
            }
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("RoadBoundary"))
        {
            return;
        }

        if (collision.contactCount <= 0)
        {
            return;
        }

        Vector3 averageNormal = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            averageNormal += collision.GetContact(i).normal;
        }

        averageNormal.Normalize();

        Vector3 horizontalVelocityDirection = new Vector3(currentHorizontalVelocity, 0f, 0f).normalized;

        if (horizontalVelocityDirection == Vector3.zero)
        {
            return;
        }

        float pushingIntoWall = Vector3.Dot(horizontalVelocityDirection, -averageNormal);

        if (pushingIntoWall > 0.2f)
        {
            currentHorizontalVelocity = 0f;
        }
    }

    private void TriggerGameOverFromObstacle(Collision collision)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)
        {
            return;
        }

        PlayCrashAnimation(collision);
    }
    
    private void PlayCrashAnimation(Collision collision)
    {
        if (isCrashing)
        {
            return;
        }

        isCrashing = true;
        SoundManager.Instance?.StopGameplayLoops();
        SoundManager.Instance?.PlayGameOver();
        currentHorizontalVelocity = 0f;
        horizontalInput = 0f;
        driftIntensity = 0f;
        driftScoreTimer = 0f;

        SetDriftDustEmission(0f);

        if (driftDustParticle != null)
        {
            driftDustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        SetStoneEmission(0f);

        if (stoneParticle != null)
        {
            stoneParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        transform.DOKill();

        if (leanPivot != null)
        {
            leanPivot.DOKill();
        }

        if (rollPivot != null)
        {
            rollPivot.DOKill();
        }

        float hitDirection = GetCrashDirection(collision);

        Vector3 startPosition = transform.position;

        Vector3 impactPosition = startPosition + new Vector3(
            hitDirection * crashSideForce,
            crashUpForce,
            -crashKnockbackZ
        );

        Vector3 fallPosition = new Vector3(
            impactPosition.x,
            Mathf.Max(0.15f, startPosition.y - crashFallDownY),
            impactPosition.z
        );

        Sequence crashSequence = DOTween.Sequence();

        crashSequence.Append(
            transform.DOMove(impactPosition, crashAnimationDuration * 0.35f)
                .SetEase(crashImpactEase)
        );

        crashSequence.Append(
            transform.DOMove(fallPosition, crashAnimationDuration * 0.65f)
                .SetEase(crashFallEase)
        );

        if (leanPivot != null)
        {
            Vector3 targetLeanRotation = new Vector3(
                65f,
                hitDirection * 25f,
                -hitDirection * crashLeanAngle
            );

            crashSequence.Join(
                leanPivot.DOLocalRotate(
                    targetLeanRotation,
                    crashAnimationDuration,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.OutCubic)
            );
        }

        if (rollPivot != null)
        {
            crashSequence.Join(
                rollPivot.DOLocalRotate(
                    new Vector3(0f, 0f, crashSpinAngle * hitDirection),
                    crashAnimationDuration,
                    RotateMode.LocalAxisAdd
                ).SetEase(Ease.OutQuart)
            );
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(crashAnimationDuration + 0.05f);
        }
    }

    private float GetCrashDirection(Collision collision)
    {
        if (collision.contactCount > 0)
        {
            Vector3 contactPoint = collision.GetContact(0).point;
            float direction = Mathf.Sign(transform.position.x - contactPoint.x);

            if (Mathf.Abs(direction) > 0.01f)
            {
                return direction;
            }
        }

        if (Mathf.Abs(currentHorizontalVelocity) > 0.05f)
        {
            return -Mathf.Sign(currentHorizontalVelocity);
        }

        return Random.value < 0.5f ? -1f : 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectPart();
            }
            SoundManager.Instance?.PlayCollectiblePickup();

            other.gameObject.SetActive(false);
        }
    }
}