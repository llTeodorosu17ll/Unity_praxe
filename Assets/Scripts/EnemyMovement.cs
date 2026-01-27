using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Agent")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Targets")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] waypoints;

    [Header("Patrol / Chase Speeds")]
    [SerializeField] private float patrolSpeed = 2.0f;   // walk
    [SerializeField] private float chaseSpeed = 4.5f;    // run

    [Header("Distances")]
    [SerializeField] private float reachDistance = 6f;       // start chasing
    [SerializeField] private float loseDistance = 12f;       // stop chasing
    [SerializeField] private float arriveDistance = 0.6f;    // waypoint arrival

    [Header("Return To Start")]
    [SerializeField] private bool returnToStart = true;
    [SerializeField] private float returnArriveDistance = 0.8f;

    [Header("Game Over")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private GameObject gameOverUI;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float chaseAnimMinSpeed = 3.2f; // forces run anim even near player

    private int waypointIndex = -1;
    private bool chasing;
    private bool returning;
    private Vector3 startPos;

    // --- Save/Load API used by SaveGameManager ---
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
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        enabled = true;

        agent.speed = chasing ? chaseSpeed : patrolSpeed;

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

        UpdateAnimator();
    }

    // --- Called by EnemyKillTrigger.cs ---
    public void OnPlayerCaught()
    {
        HandleGameOver();
    }

    // If your EnemyKillTrigger passes a Collider:
    public void OnPlayerCaught(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
            HandleGameOver(other.gameObject);
    }

    // If your EnemyKillTrigger passes a GameObject:
    public void OnPlayerCaught(GameObject playerObj)
    {
        if (playerObj != null && playerObj.CompareTag("Player"))
            HandleGameOver(playerObj);
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        startPos = transform.position;
    }

    private void Start()
    {
        startPos = transform.position;
        if (agent != null) agent.speed = patrolSpeed;
        PickNextWaypoint();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        if (player != null)
        {
            float d = Vector3.Distance(transform.position, player.position);

            if (!chasing && d <= reachDistance)
            {
                chasing = true;
                returning = false;
            }

            if (chasing && d >= loseDistance)
            {
                chasing = false;
                if (returnToStart) returning = true;
            }
        }

        agent.speed = chasing ? chaseSpeed : patrolSpeed;

        if (chasing && player != null)
        {
            agent.SetDestination(player.position);
            UpdateAnimator();
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

            UpdateAnimator();
            return;
        }

        Patrol();
        UpdateAnimator();
    }

    // Optional fallback if you still have a trigger on the root
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
            HandleGameOver(other.gameObject);
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (waypointIndex < 0 || waypointIndex >= waypoints.Length || waypoints[waypointIndex] == null)
        {
            PickNextWaypoint();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            PickNextWaypoint();

        if (waypoints[waypointIndex] != null)
            agent.SetDestination(waypoints[waypointIndex].position);
    }

    private void PickNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            if (waypoints[waypointIndex] == null) continue;

            agent.SetDestination(waypoints[waypointIndex].position);
            return;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        float v = agent.velocity.magnitude;
        if (chasing) v = Mathf.Max(v, chaseAnimMinSpeed);

        animator.SetFloat(speedParam, v);
    }

    private void HandleGameOver(GameObject playerObj = null)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (gameOverUI != null) gameOverUI.SetActive(true);

        enabled = false;
        if (agent != null) agent.isStopped = true;

        UpdateAnimator();
    }
}
