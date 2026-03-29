using UnityEngine;

[DisallowMultipleComponent]
public class EnemyVision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform eyeTransform;

    [Header("Vision")]
    [SerializeField] private float viewRadius = 6f;
    [SerializeField, Range(1f, 179f)] private float viewAngle = 70f;

    [Header("Line Of Sight")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    public Transform EyeTransform => eyeTransform != null ? eyeTransform : transform;
    public Vector3 EyeWorldPosition => EyeTransform.position;
    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;
    public LayerMask ObstacleMask => obstacleMask;

    private void Reset()
    {
        if (eyeTransform == null)
            eyeTransform = transform;
    }

    private void Awake()
    {
        if (eyeTransform == null)
            eyeTransform = transform;

        viewRadius = Mathf.Max(0.1f, viewRadius);
        viewAngle = Mathf.Clamp(viewAngle, 1f, 179f);
    }

    public bool CanSeeTarget(Transform target)
    {
        if (target == null)
            return false;

        Transform eye = EyeTransform;
        Vector3 origin = eye.position;
        Vector3 targetPoint = GetTargetPoint(target);
        Vector3 toTarget = targetPoint - origin;

        float distance = toTarget.magnitude;
        if (distance > viewRadius)
            return false;

        float angle = Vector3.Angle(eye.forward, toTarget);
        if (angle > viewAngle * 0.5f)
            return false;

        Vector3 direction = toTarget / Mathf.Max(distance, 0.0001f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            return hit.transform == target || hit.transform.IsChildOf(target);

        return true;
    }

    private Vector3 GetTargetPoint(Transform target)
    {
        Collider col = target.GetComponent<Collider>();
        if (col != null)
            return col.bounds.center;

        return target.position;
    }
}