using UnityEngine;

public class CurvedWorldController : MonoBehaviour
{
    [Header("Curve Origin")]
    [SerializeField] private Transform curveOrigin;

    [Header("Downhill Curve")]
    [SerializeField] private float baseDownCurveStrength = 0.00025f;
    [SerializeField] private float downCurveVariation = 0.00008f;
    [SerializeField] private float downCurveNoiseSpeed = 0.12f;

    [Header("Side Curve")]
    [SerializeField] private float maxSideCurveStrength = 0.00006f;
    [SerializeField] private float sideCurveNoiseSpeed = 0.08f;

    [Header("Curve Start")]
    [SerializeField] private float curveStartDistance = 10f;

    [Header("Smoothing")]
    [SerializeField] private float curveSmoothSpeed = 2f;

    [Header("Speed Sync")]
    [SerializeField] private float referenceSpeed = 10f;
    [SerializeField] private bool freezeWhenGameNotRunning = true;

    private float currentDownCurve;
    private float currentSideCurve;

    private float curveTime;

    private static readonly int CurveOriginId = Shader.PropertyToID("_CurveOrigin");
    private static readonly int GlobalCurveStrengthId = Shader.PropertyToID("_GlobalCurveStrength");
    private static readonly int GlobalCurveSideStrengthId = Shader.PropertyToID("_GlobalCurveSideStrength");
    private static readonly int GlobalCurveStartDistanceId = Shader.PropertyToID("_GlobalCurveStartDistance");

    private void Start()
    {
        currentDownCurve = baseDownCurveStrength;
        currentSideCurve = 0f;

        ApplyShaderValues();
    }

    private void LateUpdate()
    {
        if (curveOrigin == null)
        {
            return;
        }

        float speed = GetCurrentSpeed();

        if (freezeWhenGameNotRunning && !IsGameRunning())
        {
            speed = 0f;
        }

        UpdateDynamicCurveValues(speed);
        ApplyShaderValues();
    }

    private void UpdateDynamicCurveValues(float speed)
    {
        float speedRatio = 0f;

        if (referenceSpeed > 0f)
        {
            speedRatio = speed / referenceSpeed;
        }

        speedRatio = Mathf.Max(0f, speedRatio);

        curveTime += Time.deltaTime * speedRatio;

        float downNoise = Mathf.PerlinNoise(curveTime * downCurveNoiseSpeed, 10.5f);
        float sideNoise = Mathf.PerlinNoise(curveTime * sideCurveNoiseSpeed, 25.8f);

        float targetDownCurve = baseDownCurveStrength + ((downNoise - 0.5f) * 2f * downCurveVariation);
        float targetSideCurve = (sideNoise - 0.5f) * 2f * maxSideCurveStrength;

        float smoothAmount = curveSmoothSpeed * Time.deltaTime;

        currentDownCurve = Mathf.Lerp(
            currentDownCurve,
            targetDownCurve,
            smoothAmount
        );

        currentSideCurve = Mathf.Lerp(
            currentSideCurve,
            targetSideCurve,
            smoothAmount
        );
    }

    private void ApplyShaderValues()
    {
        Shader.SetGlobalVector(CurveOriginId, curveOrigin.position);
        Shader.SetGlobalFloat(GlobalCurveStrengthId, currentDownCurve);
        Shader.SetGlobalFloat(GlobalCurveSideStrengthId, currentSideCurve);
        Shader.SetGlobalFloat(GlobalCurveStartDistanceId, curveStartDistance);
    }

    private float GetCurrentSpeed()
    {
        if (GameManager.Instance == null)
        {
            return 0f;
        }

        return GameManager.Instance.currentSpeed;
    }

    private bool IsGameRunning()
    {
        if (GameManager.Instance == null)
        {
            return false;
        }

        return GameManager.Instance.isGameStarted && !GameManager.Instance.isGameOver;
    }
}