using UnityEngine;

/// <summary>
/// Enemy vision logic + one visible FOV zone in GAME view.
/// ONE script. ONE zone. Safe with Unity OnValidate (no GO creation there).
/// </summary>
public class EnemyVision : MonoBehaviour
{
    [Header("Vision shape")]
    [SerializeField] private float viewRadius = 6f;

    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 90f;

    [Header("Line of sight")]
    [Tooltip("Layers that BLOCK vision (walls, props, etc.)")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Eye position")]
    [SerializeField] private Transform eye; // optional head/eye transform
    [SerializeField] private float eyeHeight = 1.6f;

    // Public for other scripts (EnemyMovement etc.)
    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;

    // =========================
    //  GAME VIEW ZONE (URP)
    // =========================
    [Header("Visible Zone (Game View)")]
    [SerializeField] private bool showZoneInGame = true;

    [SerializeField] private Color zoneColor = new Color(1f, 0f, 0f, 0.20f); // red transparent
    [Tooltip("Lift a bit to avoid z-fighting with the floor.")]
    [SerializeField] private float zoneYOffset = 0.02f;

    [Tooltip("More segments = smoother arc.")]
    [SerializeField] private int zoneSegments = 40;

    private GameObject zoneGO;
    private MeshFilter zoneMF;
    private MeshRenderer zoneMR;
    private Mesh zoneMesh;
    private Material zoneMat;

    private bool zoneDirty = true;  // rebuild mesh when values change
    private float lastRadius = -1f;
    private float lastAngle = -1f;

    // =========================
    //  VISION LOGIC
    // =========================
    public bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        Vector3 eyePos = GetEyePos();
        Vector3 toTarget = target.position - eyePos;
        float dist = toTarget.magnitude;

        if (dist > viewRadius) return false;
        if (dist < 0.001f) return true;

        Vector3 dir = toTarget / dist;

        // Only in front (FOV cone)
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewAngle * 0.5f) return false;

        // Blocked by walls/props
        if (Physics.Raycast(eyePos, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    private Vector3 GetEyePos()
    {
        if (eye != null) return eye.position;
        return transform.position + Vector3.up * eyeHeight;
    }

    // =========================
    //  UNITY LIFECYCLE
    // =========================
    private void Awake()
    {
        // Only create runtime visuals if enabled.
        if (showZoneInGame)
            EnsureZoneCreatedRuntime();

        zoneDirty = true;
    }

    private void OnEnable()
    {
        // If enabled later at runtime, create zone then.
        if (showZoneInGame)
            EnsureZoneCreatedRuntime();

        ApplyZoneVisibility();
        zoneDirty = true;
    }

    private void OnDisable()
    {
        ApplyZoneVisibility();
    }

    private void LateUpdate()
    {
        if (!showZoneInGame)
        {
            ApplyZoneVisibility();
            return;
        }

        // Runtime-only visualizer
        EnsureZoneCreatedRuntime();
        ApplyZoneVisibility();

        if (zoneGO == null) return;

        // Follow enemy
        zoneGO.transform.position = new Vector3(transform.position.x, transform.position.y + zoneYOffset, transform.position.z);
        zoneGO.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Update material color
        if (zoneMat != null && zoneMat.color != zoneColor)
            zoneMat.color = zoneColor;

        // Rebuild if needed
        if (zoneDirty || !Mathf.Approximately(lastRadius, viewRadius) || !Mathf.Approximately(lastAngle, viewAngle))
        {
            RebuildZoneMesh();
            zoneDirty = false;
        }
    }

    private void OnDestroy()
    {
        // Cleanup runtime objects/materials (avoid playmode leaks)
        if (zoneGO != null) Destroy(zoneGO);
        if (zoneMesh != null) Destroy(zoneMesh);
        if (zoneMat != null) Destroy(zoneMat);
    }

    // IMPORTANT: OnValidate must NOT create GameObjects/components/parents.
    private void OnValidate()
    {
        viewRadius = Mathf.Max(0.1f, viewRadius);
        zoneYOffset = Mathf.Max(0f, zoneYOffset);
        zoneSegments = Mathf.Clamp(zoneSegments, 6, 256);

        // Mark dirty; runtime will rebuild safely in LateUpdate.
        zoneDirty = true;
    }

    // =========================
    //  ZONE (RUNTIME) HELPERS
    // =========================
    private void EnsureZoneCreatedRuntime()
    {
        if (zoneGO != null) return;

        zoneGO = new GameObject("Vision_Zone");
        zoneGO.transform.SetParent(transform, worldPositionStays: true);

        zoneMF = zoneGO.AddComponent<MeshFilter>();
        zoneMR = zoneGO.AddComponent<MeshRenderer>();

        zoneMesh = new Mesh { name = "Vision_Zone_Mesh" };
        zoneMF.sharedMesh = zoneMesh;

        zoneMat = CreateURPTransparentMaterial(zoneColor);
        zoneMR.sharedMaterial = zoneMat;

        zoneMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        zoneMR.receiveShadows = false;

        // Build first time
        zoneDirty = true;
    }

    private void ApplyZoneVisibility()
    {
        if (zoneGO == null) return;
        zoneGO.SetActive(showZoneInGame && enabled && gameObject.activeInHierarchy);
    }

    private void RebuildZoneMesh()
    {
        if (zoneMesh == null) return;

        lastRadius = viewRadius;
        lastAngle = viewAngle;

        BuildFovSector(zoneMesh, viewRadius, viewAngle, zoneSegments);
    }

    private void BuildFovSector(Mesh m, float radius, float angleDeg, int segments)
    {
        Vector3[] verts = new Vector3[segments + 2];
        int[] tris = new int[segments * 3];

        verts[0] = Vector3.zero;

        float half = angleDeg * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ang = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;

            // Local forward = +Z
            float x = Mathf.Sin(ang) * radius;
            float z = Mathf.Cos(ang) * radius;

            verts[i + 1] = new Vector3(x, 0f, z);
        }

        for (int i = 0; i < segments; i++)
        {
            int ti = i * 3;
            tris[ti + 0] = 0;
            tris[ti + 1] = i + 2;
            tris[ti + 2] = i + 1;
        }

        m.Clear();
        m.vertices = verts;
        m.triangles = tris;
        m.RecalculateBounds();
        m.RecalculateNormals();
    }

    // URP transparency: pick URP Unlit and set surface to transparent if possible.
    private Material CreateURPTransparentMaterial(Color c)
    {
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        Shader fallback = Shader.Find("Sprites/Default");
        Shader s = urpUnlit != null ? urpUnlit : fallback;

        Material mat = new Material(s);
        mat.name = "EnemyVisionZoneMat_Runtime";
        mat.color = c;

        // Try to force transparent rendering when URP Unlit is used
        // (properties exist depending on URP version; safe checks).
        if (urpUnlit != null)
        {
            // Common URP properties
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 1 = Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);     // Alpha
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            // Render queue to transparent
            mat.renderQueue = 3000;
        }

        return mat;
    }

#if UNITY_EDITOR
    // Scene-view gizmo (optional)
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 eyePos = GetEyePos();
        Vector3 left = DirFromAngle(-viewAngle * 0.5f);
        Vector3 right = DirFromAngle(viewAngle * 0.5f);

        Gizmos.DrawLine(eyePos, eyePos + left * viewRadius);
        Gizmos.DrawLine(eyePos, eyePos + right * viewRadius);
    }

    private Vector3 DirFromAngle(float angleDeg)
    {
        float a = (transform.eulerAngles.y + angleDeg) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
    }
#endif
}
