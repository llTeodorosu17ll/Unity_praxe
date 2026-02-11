using UnityEngine;

[DisallowMultipleComponent]
public class EnemyVisionVolumeCone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyVision vision;

    [Header("Mesh quality")]
    [SerializeField, Range(16, 256)] private int segments = 64;

    [Header("Beam volume (3D thickness)")]
    [SerializeField] private float height = 1.6f;
    [SerializeField, Range(0f, 1f)] private float verticalPivot = 0.20f;
    [SerializeField] private float yOffset = 0.02f;

    [Header("Raycast clipping")]
    [Tooltip("Small offset forward so raycasts don't hit the enemy itself.")]
    [SerializeField] private float rayStartForwardOffset = 0.15f;

    [Header("Beam look")]
    [SerializeField] private Color color = new Color(1f, 0f, 0f, 1f);
    [SerializeField, Range(0f, 5f)] private float intensity = 2.0f;
    [SerializeField, Range(0f, 1f)] private float alpha = 0.35f;
    [SerializeField, Range(0.01f, 1f)] private float edgeSoftness = 0.55f;
    [SerializeField, Range(0.5f, 8f)] private float radialPower = 1.6f;
    [SerializeField, Range(0.2f, 6f)] private float distanceFade = 0.8f;

    private GameObject coneObj;
    private MeshFilter mf;
    private MeshRenderer mr;
    private Mesh coneMesh;
    private Material runtimeMat;

    // =========================
    // Unity native methods (order)
    // =========================
    private void Awake()
    {
        if (vision == null) vision = GetComponent<EnemyVision>();

        CreateConeObjectIfNeeded();
        RebuildMesh();
        ApplyMaterialParams();
    }

    private void LateUpdate()
    {
        if (vision == null || coneObj == null) return;

        Vector3 p = vision.EyeWorldPosition;
        p.y += yOffset;

        coneObj.transform.position = p;

        float yaw = vision.EyeTransform.eulerAngles.y;
        coneObj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        RebuildMesh();
        ApplyMaterialParams();
    }

    private void OnDestroy()
    {
        if (coneMesh != null) Destroy(coneMesh);
        if (runtimeMat != null) Destroy(runtimeMat);
    }

    // =========================
    // Internal helpers
    // =========================
    private void CreateConeObjectIfNeeded()
    {
        if (coneObj != null) return;

        coneObj = new GameObject("Vision_ConeVolume");
        coneObj.transform.SetParent(transform, false);

        mf = coneObj.AddComponent<MeshFilter>();
        mr = coneObj.AddComponent<MeshRenderer>();

        coneMesh = new Mesh { name = "VisionConeClippedMesh_Runtime" };
        mf.sharedMesh = coneMesh;

        runtimeMat = CreateRuntimeMaterial();
        mr.sharedMaterial = runtimeMat;

        mr.sortingOrder = 5000;
    }

    private Material CreateRuntimeMaterial()
    {
        Shader s = Shader.Find("Custom/VolumetricFlashlightCone_NoDepthURP");
        if (s == null)
        {
            Debug.LogError("Shader not found: Custom/VolumetricFlashlightCone_NoDepthURP (check file name & compile errors).");
            s = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material mat = new Material(s);
        mat.name = "M_VisionBeam_NoDepth_Runtime";
        return mat;
    }

    private void ApplyMaterialParams()
    {
        if (mr == null || mr.sharedMaterial == null || vision == null) return;

        Material mat = mr.sharedMaterial;

        float r = Mathf.Max(0.1f, vision.ViewRadius);
        float halfWidth = Mathf.Tan(Mathf.Deg2Rad * (vision.ViewAngle * 0.5f)) * r;

        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", intensity);
        if (mat.HasProperty("_Alpha")) mat.SetFloat("_Alpha", alpha);
        if (mat.HasProperty("_EdgeSoftness")) mat.SetFloat("_EdgeSoftness", edgeSoftness);
        if (mat.HasProperty("_RadialPower")) mat.SetFloat("_RadialPower", radialPower);
        if (mat.HasProperty("_DistanceFade")) mat.SetFloat("_DistanceFade", distanceFade);

        if (mat.HasProperty("_ConeLength")) mat.SetFloat("_ConeLength", r);
        if (mat.HasProperty("_ConeHalfWidth")) mat.SetFloat("_ConeHalfWidth", Mathf.Max(0.001f, halfWidth));
    }

    private void RebuildMesh()
    {
        if (vision == null || coneMesh == null) return;

        BuildClippedWedgePrism(
            mesh: coneMesh,
            originWS: vision.EyeWorldPosition,
            forwardWS: vision.EyeTransform.forward,
            radius: vision.ViewRadius,
            angleDeg: vision.ViewAngle,
            seg: segments,
            h: height,
            pivot01: verticalPivot,
            obstacleMask: vision.ObstacleMask
        );
    }

    private void BuildClippedWedgePrism(
        Mesh mesh,
        Vector3 originWS,
        Vector3 forwardWS,
        float radius,
        float angleDeg,
        int seg,
        float h,
        float pivot01,
        LayerMask obstacleMask)
    {
        mesh.Clear();

        seg = Mathf.Max(8, seg);

        float half = angleDeg * 0.5f;
        float step = angleDeg / seg;

        float yBottom = -h * pivot01;
        float yTop = h * (1f - pivot01);

        int bottomCenter = 0;
        int bottomArcStart = 1;

        int topCenter = seg + 2;
        int topArcStart = seg + 3;

        int arcCount = seg + 1;
        int vertCount = 2 + arcCount * 2;

        Vector3[] v = new Vector3[vertCount];

        v[bottomCenter] = new Vector3(0f, yBottom, 0f);
        v[topCenter] = new Vector3(0f, yTop, 0f);

        Vector3 fwd = forwardWS.sqrMagnitude > 0.0001f ? forwardWS.normalized : transform.forward;

        // Start rays slightly forward so we don't hit the enemy's own colliders.
        Vector3 rayOriginBase = originWS + fwd * rayStartForwardOffset;

        for (int i = 0; i <= seg; i++)
        {
            float a = -half + step * i;

            Vector3 dirWS = Quaternion.AngleAxis(a, Vector3.up) * fwd;

            float dist = radius;
            if (Physics.Raycast(rayOriginBase, dirWS, out RaycastHit hit, radius, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                dist = hit.distance;
            }

            float rad = a * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * dist;
            float z = Mathf.Cos(rad) * dist;

            v[bottomArcStart + i] = new Vector3(x, yBottom, z);
            v[topArcStart + i] = new Vector3(x, yTop, z);
        }

        int[] tris = new int[
            seg * 3 +
            seg * 3 +
            seg * 6 +
            12
        ];

        int t = 0;

        for (int i = 0; i < seg; i++)
        {
            tris[t++] = bottomCenter;
            tris[t++] = bottomArcStart + i + 1;
            tris[t++] = bottomArcStart + i;
        }

        for (int i = 0; i < seg; i++)
        {
            tris[t++] = topCenter;
            tris[t++] = topArcStart + i;
            tris[t++] = topArcStart + i + 1;
        }

        for (int i = 0; i < seg; i++)
        {
            int b0 = bottomArcStart + i;
            int b1 = bottomArcStart + i + 1;
            int u0 = topArcStart + i;
            int u1 = topArcStart + i + 1;

            tris[t++] = b0; tris[t++] = b1; tris[t++] = u1;
            tris[t++] = b0; tris[t++] = u1; tris[t++] = u0;
        }

        {
            int bEdge = bottomArcStart + 0;
            int uEdge = topArcStart + 0;

            tris[t++] = bottomCenter; tris[t++] = bEdge; tris[t++] = uEdge;
            tris[t++] = bottomCenter; tris[t++] = uEdge; tris[t++] = topCenter;
        }

        {
            int bEdge = bottomArcStart + seg;
            int uEdge = topArcStart + seg;

            tris[t++] = bottomCenter; tris[t++] = uEdge; tris[t++] = bEdge;
            tris[t++] = bottomCenter; tris[t++] = topCenter; tris[t++] = uEdge;
        }

        mesh.vertices = v;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Prevent pop-in from frustum culling
        var b = mesh.bounds;
        b.Expand(new Vector3(50f, 50f, 50f));
        mesh.bounds = b;
    }
}
