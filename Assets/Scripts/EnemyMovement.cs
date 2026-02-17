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
    [SerializeField] private float patrolSpeed = 2.0f;
    [SerializeField] private float chaseSpeed = 4.5f;

    [Header("Distances")]
    [SerializeField] private float reachDistance = 6f;
    [SerializeField] private float loseDistance = 12f;
    [SerializeField] private float arriveDistance = 0.6f;

    [Header("Return To Start")]
    [SerializeField] private bool returnToStart = true;
    [SerializeField] private float returnArriveDistance = 0.8f;

    [Header("Idle Thinking")]
    [SerializeField] private bool enableThinkIdle = true;
    [SerializeField] private float startThinkSeconds = 0.5f;
    [SerializeField] private float thinkSecondsMin = 0.6f;
    [SerializeField] private float thinkSecondsMax = 1.4f;

    [Header("Game Over")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private GameObject gameOverUI;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float chaseAnimMinSpeed = 3.2f;

    [Header("Vision")]
    [SerializeField] private EnemyVision vision;

    [Header("Detection Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip detectionClip;

    private EnemyVisionVolumeCone visionCone;

    private int waypointIndex = -1;
    private bool chasing;
    private bool returning;
    private Vector3 startPos;

    private bool isThinking;
    private float thinkTimer;

    private bool wasChasingLastFrame;

    public bool IsChasingPlayer => chasing;
    public bool IsReturning => returning;
    public int CurrentWaypointIndex => waypointIndex;

    public void ApplySavedAIState(bool chasingPlayer, bool isReturning, int savedWaypointIndex)
    {
        chasing = chasingPlayer;
        returning = isReturning;

        isThinking = false;
        thinkTimer = 0f;

        if (waypoints != null && waypoints.Length > 0)
            waypointIndex = Mathf.Clamp(savedWaypointIndex, 0, waypoints.Length - 1);
        else
            waypointIndex = -1;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (vision == null) vision = GetComponent<EnemyVision>();

        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.speed = chasing ? chaseSpeed : patrolSpeed;

        if (chasing && player != null)
            agent.SetDestination(player.position);
        else if (returning)
            agent.SetDestination(startPos);
        else
            PickNextWaypoint();

        UpdateAnimator();
    }

    public void OnPlayerCaught() => HandleGameOver();

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
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        visionCone = GetComponentInChildren<EnemyVisionVolumeCone>(true);

        startPos = transform.position;
    }

    private void Start()
    {
        if (agent != null)
            agent.speed = patrolSpeed;

        if (visionCone != null)
            visionCone.SetVisible(false);

        if (enableThinkIdle && startThinkSeconds > 0f)
            BeginThinking(startThinkSeconds);
        else
            PickNextWaypoint();

        UpdateAnimator();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        if (isThinking)
        {
            TickThinking();
            UpdateAnimator();
            return;
        }

        bool seesPlayer = false;

        if (player != null)
        {
            if (vision != null)
                seesPlayer = vision.CanSeeTarget(player);
            else
                seesPlayer = Vector3.Distance(transform.position, player.position) <= reachDistance;
        }

        if (!chasing && seesPlayer)
        {
            chasing = true;
            returning = false;
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

                if (enableThinkIdle)
                    BeginThinking(RandomThinkTime());
            }
        }

        if (chasing && !wasChasingLastFrame)
        {
            if (audioSource != null && detectionClip != null)
                audioSource.PlayOneShot(detectionClip);
        }

        if (visionCone != null)
            visionCone.SetVisible(!chasing);

        wasChasingLastFrame = chasing;

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

    private void BeginThinking(float seconds)
    {
        isThinking = true;
        thinkTimer = seconds;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void TickThinking()
    {
        thinkTimer -= Time.deltaTime;

        if (thinkTimer <= 0f)
        {
            isThinking = false;

            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = false;

            PickNextWaypoint();
        }
    }

    private float RandomThinkTime()
    {
        return Random.Range(thinkSecondsMin, thinkSecondsMax);
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            if (enableThinkIdle)
                BeginThinking(RandomThinkTime());
            else
                PickNextWaypoint();
        }
    }

    private void PickNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        waypointIndex = (waypointIndex + 1) % waypoints.Length;
        agent.SetDestination(waypoints[waypointIndex].position);
    }

    private void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        float speed = isThinking ? 0f : agent.velocity.magnitude;

        if (chasing)
            speed = Mathf.Max(speed, chaseAnimMinSpeed);

        animator.SetFloat(speedParam, speed);
    }

    private void HandleGameOver(GameObject playerObj = null)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        enabled = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}
