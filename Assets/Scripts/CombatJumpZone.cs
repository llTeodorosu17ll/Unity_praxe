using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CombatJumpZone : MonoBehaviour
{
    [Header("Landing points (set these in Inspector)")]
    [SerializeField] private Transform landingPointA;
    [SerializeField] private Transform landingPointB;

    [Header("Jump feel")]
    [SerializeField] private float travelTime = 0.35f;
    [SerializeField] private float arcHeight = 0.9f;

    [Header("Rules")]
    [Tooltip("Extra safety: if player is too far from zone center, don't allow jump (even if collider is huge).")]
    [SerializeField] private float maxUseDistanceFromZoneCenter = 4.0f;

    [Header("Animation (optional)")]
    [Tooltip("If your Animator has this trigger, it will be used. Otherwise fallback to normal Jump trigger.")]
    [SerializeField] private string combatJumpTrigger = "CombatJump";

    public float TravelTime => travelTime;
    public float ArcHeight => arcHeight;
    public float MaxUseDistanceFromZoneCenter => maxUseDistanceFromZoneCenter;
    public string CombatJumpTrigger => combatJumpTrigger;

    public bool IsValid => landingPointA != null && landingPointB != null;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;

        pm.RegisterCombatJumpZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;

        pm.UnregisterCombatJumpZone(this);
    }

    public bool CanUseFrom(Vector3 playerPos)
    {
        return FlatDistance(playerPos, transform.position) <= Mathf.Max(0.1f, maxUseDistanceFromZoneCenter);
    }

    public Transform GetOtherSideLanding(Vector3 playerPos)
    {
        if (!IsValid) return null;

        float dA = FlatDistance(playerPos, landingPointA.position);
        float dB = FlatDistance(playerPos, landingPointB.position);

        // choose the one farther away -> "jump to the other side"
        return (dA >= dB) ? landingPointA : landingPointB;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxUseDistanceFromZoneCenter);

        Gizmos.color = Color.green;
        if (landingPointA != null) Gizmos.DrawSphere(landingPointA.position, 0.12f);
        if (landingPointB != null) Gizmos.DrawSphere(landingPointB.position, 0.12f);
    }
}