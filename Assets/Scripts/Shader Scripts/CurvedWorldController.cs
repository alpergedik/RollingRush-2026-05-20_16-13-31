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

    private float currentDownCurve;
    private float currentSideCurve;

    private static readonly int CurveOriginId = Shader.PropertyToID("_CurveOrigin");
    private static readonly int GlobalCurveStrengthId = Shader.PropertyToID("_GlobalCurveStrength");
    private static readonly int GlobalCurveSideStrengthId = Shader.PropertyToID("_GlobalCurveSideStrength");
    private static readonly int GlobalCurveStartDistanceId = Shader.PropertyToID("_GlobalCurveStartDistance");

    private void Start()
    {
        currentDownCurve = baseDownCurveStrength;
        currentSideCurve = 0f;
    }

    private void LateUpdate()
    {
        if (curveOrigin == null)
        {
            return;
        }

        UpdateDynamicCurveValues();

        Shader.SetGlobalVector(CurveOriginId, curveOrigin.position);
        Shader.SetGlobalFloat(GlobalCurveStrengthId, currentDownCurve);
        Shader.SetGlobalFloat(GlobalCurveSideStrengthId, currentSideCurve);
        Shader.SetGlobalFloat(GlobalCurveStartDistanceId, curveStartDistance);
    }

    private void UpdateDynamicCurveValues()
    {
        float time = Time.time;

        float downNoise = Mathf.PerlinNoise(time * downCurveNoiseSpeed, 10.5f);
        float sideNoise = Mathf.PerlinNoise(time * sideCurveNoiseSpeed, 25.8f);

        float targetDownCurve = baseDownCurveStrength + ((downNoise - 0.5f) * 2f * downCurveVariation);
        float targetSideCurve = (sideNoise - 0.5f) * 2f * maxSideCurveStrength;

        currentDownCurve = Mathf.Lerp(
            currentDownCurve,
            targetDownCurve,
            curveSmoothSpeed * Time.deltaTime
        );

        currentSideCurve = Mathf.Lerp(
            currentSideCurve,
            targetSideCurve,
            curveSmoothSpeed * Time.deltaTime
        );
    }
}