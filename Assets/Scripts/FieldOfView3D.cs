using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FieldOfView3D : MonoBehaviour
{
    [Header("Vision")]
    [Min(0f)] public float viewRadius = 8f;
    [Range(0f, 360f)] public float viewAngle = 90f;

    [Header("Masks")]
    public LayerMask targetMask;    // assign to Targets
    public LayerMask obstacleMask;  // assign to Obstacles

    [Header("Mesh")]
    [Range(0.01f, 1f)] public float meshDensity = 0.2f;
    [Range(1, 10)] public int edgeResolveIterations = 4;
    [Range(0.001f, 1f)] public float edgeDistThreshold = 0.5f;

    [Header("Detection")]
    public Color normalColor = Color.white; // Will be set to material's default color in Start
    public Color alertColor = Color.red;

    Mesh _mesh;
    readonly List<Vector3> _viewPoints = new();
    Collider[] _targetsBuffer = new Collider[64];
    MeshRenderer _meshRenderer;

    public List<Transform> visibleTargets { get; private set; } = new();
    public bool isPlayerDetected { get; private set; } = false;

    void Awake()
    {
        _mesh = new Mesh { name = "FOV Mesh" };
        GetComponent<MeshFilter>().mesh = _mesh;
        _meshRenderer = GetComponent<MeshRenderer>();
        
        // Store the material's default color as normal color
        if (_meshRenderer != null && _meshRenderer.material != null)
        {
            normalColor = _meshRenderer.material.color;
        }
    }

    void LateUpdate()
    {
        FindVisibleTargets();
        DrawFOVMesh();
        UpdateDetectionState();
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Vector3 pos = transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            pos, viewRadius, _targetsBuffer, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Transform t = _targetsBuffer[i].transform;
            
            // Debug: Log what objects are found in the overlap sphere
            Debug.Log($"Found object in FOV: {t.name}, Tag: {t.tag}");
            
            // Only detect objects tagged as "Player"
            if (!t.CompareTag("Player"))
            {
                Debug.Log($"Object {t.name} is not tagged as Player, skipping");
                continue;
            }
                
            Vector3 dirToTarget = (t.position - pos).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            Debug.Log($"Player {t.name} found! Distance: {Vector3.Distance(pos, t.position)}, Angle: {angleToTarget}");

            if (angleToTarget <= viewAngle * 0.5f)
            {
                float distToTarget = Vector3.Distance(pos, t.position);
                
                // Debug raycast
                Debug.DrawRay(pos, dirToTarget * distToTarget, Color.yellow, 0.1f);
                
                if (Physics.Raycast(pos, dirToTarget, out RaycastHit hit, distToTarget, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.Log($"Player {t.name} is blocked by obstacle: {hit.collider.name} at distance {hit.distance}");
                    Debug.DrawRay(pos, dirToTarget * hit.distance, Color.red, 0.1f);
                }
                else
                {
                    visibleTargets.Add(t);
                    Debug.Log($"Player {t.name} added to visible targets! (No obstacle blocking)");
                }
            }
            else
            {
                Debug.Log($"Player {t.name} is outside view angle (current: {angleToTarget}, max: {viewAngle * 0.5f})");
            }
        }
    }

    void UpdateDetectionState()
    {
        bool wasDetected = isPlayerDetected;
        isPlayerDetected = visibleTargets.Count > 0;
        
        // Update color based on detection state
        if (_meshRenderer != null)
        {
            _meshRenderer.material.color = isPlayerDetected ? alertColor : normalColor;
        }
        
        // Notify other scripts if detection state changed
        if (wasDetected != isPlayerDetected)
        {
            OnDetectionStateChanged?.Invoke(isPlayerDetected);
        }
    }

    // Event for when detection state changes
    public System.Action<bool> OnDetectionStateChanged;

    struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;
        public ViewCastInfo(bool h, Vector3 p, float d, float a) { hit = h; point = p; distance = d; angle = a; }
    }

    void DrawFOVMesh()
    {
        _viewPoints.Clear();

        int stepCount = Mathf.CeilToInt(viewAngle / Mathf.Max(0.01f, meshDensity));
        float stepAngleSize = viewAngle / stepCount;

        ViewCastInfo prevCast = default;
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2f + stepAngleSize * i;
            ViewCastInfo newCast = ViewCast(angle);

            if (i > 0)
            {
                bool thresholdExceeded = Mathf.Abs(prevCast.distance - newCast.distance) > edgeDistThreshold;
                if (prevCast.hit != newCast.hit || (prevCast.hit && newCast.hit && thresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(prevCast, newCast);
                    if (edge.pointA != Vector3.zero) _viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) _viewPoints.Add(edge.pointB);
                }
            }

            _viewPoints.Add(newCast.point);
            prevCast = newCast;
        }

        int vertexCount = _viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < _viewPoints.Count; i++)
            vertices[i + 1] = transform.InverseTransformPoint(_viewPoints[i]);

        int triIndex = 0;
        for (int i = 0; i < vertexCount - 2; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 1;
            triangles[triIndex++] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }

    struct EdgeInfo { public Vector3 pointA, pointB; public EdgeInfo(Vector3 a, Vector3 b) { pointA = a; pointB = b; } }

    EdgeInfo FindEdge(ViewCastInfo minCast, ViewCastInfo maxCast)
    {
        float minAngle = minCast.angle;
        float maxAngle = maxCast.angle;
        Vector3 minPoint = Vector3.zero, maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) * 0.5f;
            ViewCastInfo newCast = ViewCast(angle);

            bool thresholdExceeded = Mathf.Abs(minCast.distance - newCast.distance) > edgeDistThreshold;
            if (newCast.hit == minCast.hit && !thresholdExceeded) { minAngle = angle; minPoint = newCast.point; }
            else { maxAngle = angle; maxPoint = newCast.point; }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle);
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, viewRadius, obstacleMask, QueryTriggerInteraction.Ignore))
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);

        return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
    }

    static Vector3 DirFromAngle(float angleInDegrees)
    {
        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
#endif
}
