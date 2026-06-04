using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SkidMarkController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform skidPoint;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Skid Settings")]
    [SerializeField, Min(0.001f)] private float minPointDistance = 0.08f;
    [SerializeField, Min(0.01f)] private float pointLifetime = 0.6f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.01f;
    [SerializeField, Min(0f)] private float rayStartHeight = 1f;
    [SerializeField, Min(0.01f)] private float rayDistance = 3f;
    [SerializeField, Min(2)] private int maxPointCount = 256;

    [Header("World Movement")]
    [SerializeField] private Vector3 worldMoveDirection = Vector3.back;

    [Header("Debug")]
    [SerializeField] private bool debugSkid = false;
    [SerializeField, Min(0.1f)] private float warningLogInterval = 1f;
    
    [SerializeField, Min(0.005f)]
    private float maxSegmentLength = 0.025f;

    private sealed class SkidPointData
    {
        public Vector3 Position;
        public float RemainingLifetime;

        public SkidPointData(Vector3 position, float remainingLifetime)
        {
            Position = position;
            RemainingLifetime = remainingLifetime;
        }
    }

    private readonly List<SkidPointData> points = new List<SkidPointData>();

    private bool wasDrifting;
    private float nextWarningLogTime;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        if (skidPoint == null)
        {
            skidPoint = transform;
        }

        if (lineRenderer == null)
        {
            Debug.LogError(
                "[SkidMarkController] LineRenderer bulunamadı.",
                this
            );

            enabled = false;
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 0;
    }

    private void OnDisable()
    {
        ClearTrail();
        wasDrifting = false;
    }

    private void Update()
    {
        if (!HasRequiredReferences())
        {
            LogMissingReferences();
            return;
        }

        MoveExistingPoints();
        UpdatePointLifetimes();

        bool shouldCreateSkid = playerController.IsDriftingForFX;

        if (shouldCreateSkid && !wasDrifting)
        {
            ClearTrail();

            if (debugSkid)
            {
                Debug.Log(
                    "[SkidMarkController] Yeni drift başladı.",
                    this
                );
            }
        }

        if (shouldCreateSkid)
        {
            TryAddSkidPoint();
        }

        UpdateLineRenderer();

        wasDrifting = shouldCreateSkid;
    }

    private bool HasRequiredReferences()
    {
        return
            playerController != null &&
            skidPoint != null &&
            lineRenderer != null;
    }

    private void LogMissingReferences()
    {
        if (!debugSkid || Time.time < nextWarningLogTime)
        {
            return;
        }

        nextWarningLogTime = Time.time + warningLogInterval;

        Debug.LogWarning(
            $"[SkidMarkController] Eksik referans. " +
            $"PlayerController={(playerController != null)}, " +
            $"SkidPoint={(skidPoint != null)}, " +
            $"LineRenderer={(lineRenderer != null)}",
            this
        );
    }

    private void MoveExistingPoints()
    {
        if (points.Count == 0)
        {
            return;
        }

        Vector3 direction = worldMoveDirection.sqrMagnitude > 0.0001f
            ? worldMoveDirection.normalized
            : Vector3.back;

        Vector3 movement =
            direction *
            playerController.CurrentForwardSpeed *
            Time.deltaTime;

        for (int i = 0; i < points.Count; i++)
        {
            points[i].Position += movement;
        }
    }

    private void UpdatePointLifetimes()
    {
        for (int i = points.Count - 1; i >= 0; i--)
        {
            points[i].RemainingLifetime -= Time.deltaTime;

            if (points[i].RemainingLifetime <= 0f)
            {
                points.RemoveAt(i);
            }
        }
    }

    private void TryAddSkidPoint()
    {
        if (!TryGetGroundPoint(out Vector3 groundPoint))
        {
            ClearTrail();
            return;
        }

        if (points.Count == 0)
        {
            Vector3 direction = worldMoveDirection.sqrMagnitude > 0.0001f
                ? worldMoveDirection.normalized
                : Vector3.back;

            AddPoint(groundPoint + direction * 0.02f);
            AddPoint(groundPoint);
            return;
        }

        Vector3 lastPoint = points[points.Count - 1].Position;
        float distance = Vector3.Distance(lastPoint, groundPoint);

        if (distance < minPointDistance)
        {
            return;
        }

        int stepCount = Mathf.Max(
            1,
            Mathf.CeilToInt(distance / maxSegmentLength)
        );

        for (int i = 1; i <= stepCount; i++)
        {
            float t = i / (float)stepCount;

            Vector3 interpolatedPoint = Vector3.Lerp(
                lastPoint,
                groundPoint,
                t
            );

            if (points.Count >= maxPointCount)
            {
                points.RemoveAt(0);
            }

            AddPoint(interpolatedPoint);
        }
    }

    private void AddPoint(Vector3 position)
    {
        points.Add(new SkidPointData(position, pointLifetime));
    }

    private bool TryGetGroundPoint(out Vector3 groundPoint)
    {
        Vector3 origin =
            skidPoint.position +
            Vector3.up * rayStartHeight;

        bool hitGround = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (hitGround)
        {
            groundPoint =
                hit.point +
                hit.normal * surfaceOffset;

            if (debugSkid)
            {
                Debug.DrawLine(
                    origin,
                    hit.point,
                    Color.yellow
                );
            }

            return true;
        }

        groundPoint = Vector3.zero;

        if (debugSkid)
        {
            Debug.DrawLine(
                origin,
                origin + Vector3.down * rayDistance,
                Color.red
            );
        }

        return false;
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        if (points.Count < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(
                i,
                points[i].Position
            );
        }
    }

    private void ClearTrail()
    {
        points.Clear();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }
}