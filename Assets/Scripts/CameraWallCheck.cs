using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraWallCheck : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float sphereRadius = 0.20f;
    [SerializeField] private float wallOffset = 0.12f;
    [SerializeField] private float minDistance = 1.0f;

    [Header("Smoothing")]
    [SerializeField] private float distanceSmooth = 12f;

    private float currentDistance = -1f;

    private void Awake()
    {
        TryResolveTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
            TryResolveTarget();

        if (target == null)
            return;

        Vector3 origin = target.position;
        Vector3 desiredPos = transform.position;

        Vector3 toCam = desiredPos - origin;
        float desiredDistance = toCam.magnitude;
        if (desiredDistance < 0.001f)
            return;

        Vector3 direction = toCam / desiredDistance;

        if (currentDistance < 0f)
            currentDistance = desiredDistance;

        float targetDistance = desiredDistance;

        Vector3 castOrigin = origin + direction * 0.05f;
        float castDistance = Mathf.Max(0f, desiredDistance - 0.05f);

        if (Physics.SphereCast(
                castOrigin,
                sphereRadius,
                direction,
                out RaycastHit hit,
                castDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            targetDistance = Mathf.Max(minDistance, hit.distance + 0.05f - wallOffset);
        }

        currentDistance = Mathf.Lerp(
            currentDistance,
            targetDistance,
            1f - Mathf.Exp(-distanceSmooth * Time.unscaledDeltaTime));

        transform.position = origin + direction * currentDistance;
    }

    private void TryResolveTarget()
    {
        if (target != null)
            return;

        if (!GameManager.HasInstance || GameManager.Instance.PlayerTransform == null)
            return;

        Transform player = GameManager.Instance.PlayerTransform;
        Transform cameraTarget = player.Find("CameraTarget");
        target = cameraTarget != null ? cameraTarget : player;
    }
}