using UnityEngine;

public class EnemyKillTrigger : MonoBehaviour
{
    [SerializeField] private EnemyMovement enemy;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponentInParent<EnemyMovement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (enemy != null) enemy.OnPlayerCaught(other.gameObject);
    }
}
