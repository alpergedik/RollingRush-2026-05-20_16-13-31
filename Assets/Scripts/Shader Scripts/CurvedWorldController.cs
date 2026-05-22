using UnityEngine;

public class CurvedWorldController : MonoBehaviour
{
    [Header("Curve Origin")]
    [SerializeField] private Transform curveOrigin;

    private static readonly int CurveOriginId = Shader.PropertyToID("_CurveOrigin");

    private void LateUpdate()
    {
        if (curveOrigin == null)
        {
            return;
        }

        Shader.SetGlobalVector(CurveOriginId, curveOrigin.position);
    }
}