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
    [Tooltip("Fallback start-chase distance if EnemyVision is missing.")]
    [SerializeField] private float reachDistance = 6f;       // start chasing (fallback)
    [SerializeField] private float loseDistance = 12f;       // stop chasing
    [SerializeField] private float arriveDistance = 0.6f;    // waypoint arrival

    [Header("Return To Start")]
    [SerializeField] private bool returnToStart = true;
    [SerializeField] private float returnArriveDistance = 0.8f;

    [Header("Idle 'Thinking' at Waypoints")]
    [Tooltip("Enemy will stop (Idle) before choosing next destination.")]
    [SerializeField] private bool enableThinkIdle = true;

    [Tooltip("Idle at the very beginning before first move.")]
    [SerializeField] private float startThinkSeconds = 0.5f;

    [Tooltip("Min idle time at waypoint.")]
    [SerializeField] private float thinkSecondsMin = 0.6f;

    [Tooltip("Max idle time at waypoint.")]
    [SerializeField] private float thinkSecondsMax = 1.4f;

    [Header("Game Over")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private GameObject gameOverUI;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float chaseAnimMinSpeed = 3.2f; // forces run anim even near player

    [Header("Vision (FOV + walls)")]
    [SerializeField] private EnemyVision vision; // if present: used for FOV + wall blocking

    private int waypointIndex = -1;
    private bool chasing;
    private bool returning;
    private Vector3 startPos;

    // Thinking/idle state
    private bool isThinking;
    private float thinkTimer;

    // --- Save/Load API used by SaveGameManager ---
    public bool IsChasingPlayer => chasing;
    public bool IsReturning => returning;
    public int CurrentWaypointIndex => waypointIndex;

    public void ApplySavedAIState(bool chasingPlayer, bool isReturning, int savedWaypointIndex)
    {
        chasing = chasingPlayer;
        returning = isReturning;

        // Thinking state is not persisted; we reset it safely.
        isThinking = false;
        thinkTimer = 0f;

        if (waypoints != null && waypoints.Length > 0)
            waypointIndex = Mathf.Clamp(savedWaypointIndex, 0, waypoints.Length - 1);
        else
            waypointIndex = -1;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (vision == null) vision = GetComponent<EnemyVision>();

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

    public void OnPlayerCaught(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
            HandleGameOver(other.gameObject);
    }

    public void OnPlayerCaught(GameObject playerObj)
    {
        if (playerObj != null && playerObj.CompareTag("Player"))
            HandleGameOver(playerObj);
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (vision == null) vision = GetComponent<EnemyVision>();

        startPos = transform.position;
    }

    private void Start()
    {
        startPos = transform.position;

        if (agent != null)
            agent.speed = patrolSpeed;

        // Start with thinking idle (optional)
        if (enableThinkIdle && startThinkSeconds > 0f)
        {
            BeginThinking(startThinkSeconds);
        }
        else
        {
            PickNextWaypoint();
        }

        UpdateAnimator();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        // If thinking: stay idle, then continue patrol
        if (isThinking)
        {
            TickThinking();
            UpdateAnimator();
            return;
        }

        // --- Vision-based chase start (FOV + walls) ---
        bool seesPlayer = false;

        if (player != null)
        {
            if (vision != null)
            {
                seesPlayer = vision.CanSeeTarget(player);
            }
            else
            {
                float dFallback = Vector3.Distance(transform.position, player.position);
                seesPlayer = dFallback <= reachDistance;
            }
        }

        if (!chasing && seesPlayer)
        {
            chasing = true;
            returning = false;

            // if we were about to think, cancel it
            isThinking = false;
            thinkTimer = 0f;
        }

        if (chasing && player != null && !seesPlayer)
        {
            float d = Vector3.Distance(transform.position, player.position);
            if (d >= loseDistance)
            {
                chasing = false;
                if (returnToStart) returning = true;

                // after losing player, optional small "thinking" before moving (looks natural)
                if (enableThinkIdle)
                    BeginThinking(RandomThinkTime());
            }
        }

        agent.speed = chasing ? chaseSpeed : patrolSpeed;

        if (chasing && player != null)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            UpdateAnimator();
            return;
        }

        if (returning)
        {
            agent.isStopped = false;
            agent.SetDestination(startPos);

            if (!agent.pathPending && agent.remainingDistance <= returnArriveDistance)
            {
                returning = false;

                if (enableThinkIdle)
                    BeginThinking(RandomThinkTime());
                else
                    PickNextWaypoint();
            }

            UpdateAnimator();
            return;
        }

        Patrol();
        UpdateAnimator();
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (waypointIndex < 0 || waypointIndex >= waypoints.Length || waypoints[waypointIndex] == null)
        {
            PickNextWaypoint();
            return;
        }

        agent.isStopped = false;

        // Arrived -> think idle -> then choose next waypoint
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            if (enableThinkIdle)
                BeginThinking(RandomThinkTime());
            else
                PickNextWaypoint();

            return;
        }

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

            agent.isStopped = false;
            agent.SetDestination(waypoints[waypointIndex].position);
            return;
        }
    }

    private void BeginThinking(float seconds)
    {
        isThinking = true;
        thinkTimer = Mathf.Max(0f, seconds);

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void TickThinking()
    {
        thinkTimer -= Time.deltaTime;
        if (thinkTimer > 0f) return;

        isThinking = false;
        thinkTimer = 0f;

        // Continue patrol after thinking
        PickNextWaypoint();
    }

    private float RandomThinkTime()
    {
        float min = Mathf.Max(0f, thinkSecondsMin);
        float max = Mathf.Max(min, thinkSecondsMax);
        return Random.Range(min, max);
    }

    private void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        // If thinking (idle), speed should be 0
        float v = 0f;

        if (!isThinking)
        {
            v = agent.velocity.magnitude;
            if (chasing) v = Mathf.Max(v, chaseAnimMinSpeed);
        }

        animator.SetFloat(speedParam, v);
    }

    private void HandleGameOver(GameObject playerObj = null)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (gameOverUI != null) gameOverUI.SetActive(true);

        enabled = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        UpdateAnimator();
    }
}
