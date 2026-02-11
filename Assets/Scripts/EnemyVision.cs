using UnityEngine;

[DisallowMultipleComponent]
public class EnemyVision : MonoBehaviour
{
    [Header("Vision shape")]
    [SerializeField] private float viewRadius = 6f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 90f;

    [Header("Line of sight")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Eye position")]
    [SerializeField] private Transform eye;
    [SerializeField] private float eyeHeight = 0f;

    // =========================
    // Unity native methods (order)
    // =========================
    private void Awake()
    {
        if (eye == null) eye = transform;
    }

    // =========================
    // Public API
    // =========================
    public bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        Vector3 origin = EyeWorldPosition;
        Vector3 toTarget = target.position - origin;

        if (toTarget.sqrMagnitude > viewRadius * viewRadius) return false;

        Vector3 dir = toTarget.normalized;
        float angleToTarget = Vector3.Angle(transform.forward, dir);
        if (angleToTarget > viewAngle * 0.5f) return false;

        float distance = toTarget.magnitude;
        if (Physics.Raycast(origin, dir, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;
    public LayerMask ObstacleMask => obstacleMask;

    public Transform EyeTransform => eye != null ? eye : transform;

    public Vector3 EyeWorldPosition
    {
        get
        {
            Vector3 p = EyeTransform.position;
            p.y += eyeHeight;
            return p;
        }
    }
}
