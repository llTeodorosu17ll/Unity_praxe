using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float speed = 3f;

    [Header("Distances")]
    [SerializeField] private float reachDistance = 5f;     
    [SerializeField] private float arriveDistance = 0.3f;  
    [SerializeField] private float breakDistance = 20f;    

    [Header("Targets")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Transform baseLocation;
    [SerializeField] private Transform player;

    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private GameObject gameOverUI;


    private Transform currentTarget;
    private int lastIndex = -1;

    private void Start()
    {
        PickRandomTarget();
    }

    private void Update()
    {
        if (currentTarget == null) return;

        float distanceToPlayer = player != null ? Vector3.Distance(player.position, transform.position) : float.PositiveInfinity;
        float distanceToBase = baseLocation != null ? Vector3.Distance(baseLocation.position, transform.position) : 0f;

        if (distanceToPlayer < reachDistance)
        {
            currentTarget = player;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget.position,
            speed * Time.deltaTime
        );

        if (currentTarget != player && Vector3.Distance(transform.position, currentTarget.position) <= arriveDistance)
        {
            PickRandomTarget();
            return;
        }

        if (currentTarget == player && baseLocation != null && distanceToBase > breakDistance)
        {
            PickRandomTarget();
            return;
        }
    }

    private void PickRandomTarget()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (waypoints.Length == 1)
        {
            currentTarget = waypoints[0];
            return;
        }

        int index;
        do
        {
            index = Random.Range(0, waypoints.Length);
        } while (index == lastIndex);

        lastIndex = index;
        currentTarget = waypoints[index];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        enabled = false;
    }

}
