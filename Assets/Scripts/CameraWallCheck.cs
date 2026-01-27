using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraWallCheck : MonoBehaviour
{
    [Header("Target (Player/CameraTarget)")]
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
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var t = player.transform.Find("CameraTarget");
                target = t != null ? t : player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 origin = target.position;

        Vector3 desiredPos = transform.position;

        Vector3 toCam = desiredPos - origin;
        float desiredDist = toCam.magnitude;
        if (desiredDist < 0.001f) return;

        Vector3 dir = toCam / desiredDist;

        if (currentDistance < 0f) currentDistance = desiredDist;

        float targetDist = desiredDist;

        Vector3 castOrigin = origin + dir * 0.05f;
        float castDist = Mathf.Max(0f, desiredDist - 0.05f);

        if (Physics.SphereCast(castOrigin, sphereRadius, dir, out RaycastHit hit, castDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            targetDist = Mathf.Max(minDistance, hit.distance + 0.05f - wallOffset);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDist, 1f - Mathf.Exp(-distanceSmooth * Time.unscaledDeltaTime));

        Vector3 correctedPos = origin + dir * currentDistance;
        transform.position = correctedPos;
    }
}
