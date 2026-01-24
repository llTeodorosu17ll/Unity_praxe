using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Agent")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Distances")]
    [SerializeField] private float reachDistance = 6f;     // start chasing
    [SerializeField] private float loseDistance = 12f;     // stop chasing
    [SerializeField] private float arriveDistance = 0.6f;  // waypoint arrival

    [Header("Return To Start")]
    [SerializeField] private bool returnToStart = true;
    [SerializeField] private float returnArriveDistance = 0.8f;

    [Header("Targets")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] waypoints;

    [Header("Game Over")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private GameObject gameOverUI;

    private int waypointIndex = -1;
    private bool chasing;

    private Vector3 startPos;
    private bool returning;

    // --- Save/Load API ---
    public bool IsChasingPlayer => chasing;
    public bool IsReturning => returning;
    public int CurrentWaypointIndex => waypointIndex;

    public void ApplySavedAIState(bool chasingPlayer, bool isReturning, int savedWaypointIndex)
    {
        chasing = chasingPlayer;
        returning = isReturning;

        if (waypoints != null && waypoints.Length > 0)
            waypointIndex = Mathf.Clamp(savedWaypointIndex, 0, waypoints.Length - 1);
        else
            waypointIndex = -1;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        enabled = true;

        if (chasing && player != null)
        {
            agent.SetDestination(player.position);
        }
        else if (returning)
        {
            agent.SetDestination(startPos);
        }
        else if (waypoints != null && waypoints.Length > 0 && waypointIndex >= 0 && waypoints[waypointIndex] != null)
        {
            agent.SetDestination(waypoints[waypointIndex].position);
        }
        else
        {
            PickNextWaypoint();
        }
    }

    // --- Unity event functions ---
    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        startPos = transform.position;
    }

    private void Start()
    {
        startPos = transform.position;
        PickNextWaypoint();
    }

    private void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        float d = Vector3.Distance(transform.position, player.position);

        if (!chasing && d <= reachDistance)
        {
            chasing = true;
            returning = false;
        }

        if (chasing && d >= loseDistance)
        {
            chasing = false;
            if (returnToStart)
                returning = true;
        }

        if (chasing)
        {
            agent.SetDestination(player.position);
            return;
        }

        if (returning)
        {
            agent.SetDestination(startPos);

            if (!agent.pathPending && agent.remainingDistance <= returnArriveDistance)
            {
                returning = false;
                PickNextWaypoint();
            }
            return;
        }

        Patrol();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (gameOverUI != null) gameOverUI.SetActive(true);

        enabled = false;
        if (agent != null) agent.isStopped = true;
    }

    // --- Patrol helpers ---
    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform wp = waypoints[waypointIndex];
        if (wp == null) { PickNextWaypoint(); return; }

        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            PickNextWaypoint();

        agent.SetDestination(wp.position);
    }

    private void PickNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        waypointIndex = (waypointIndex + 1) % waypoints.Length;
        if (waypoints[waypointIndex] != null)
            agent.SetDestination(waypoints[waypointIndex].position);
    }
}
