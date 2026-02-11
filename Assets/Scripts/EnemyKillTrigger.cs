using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyKillTrigger : MonoBehaviour
{
    [SerializeField] private EnemyMovement enemy;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<EnemyMovement>();

        Collider col = GetComponent<Collider>();
        col.enabled = true;
        col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (enemy == null) return;

        enemy.OnPlayerCaught(other.gameObject);
    }
}
