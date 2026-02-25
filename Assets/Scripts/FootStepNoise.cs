using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class FootStepNoise : MonoBehaviour
{
    [Header("Step Timing")]
    [SerializeField] private float walkInterval = 0.425f;
    [SerializeField] private float runInterval = 0.4f;
    [SerializeField] private float crouchInterval = 0.45f;

    private AudioSource audioSource;
    private CharacterController controller;
    private NavMeshAgent agent;
    private PlayerMovement playerMovement;

    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controller = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        playerMovement = GetComponent<PlayerMovement>();

        // IMPORTANT:
        // Do NOT override rolloff or distances anymore.
        // Use whatever you configured in Inspector.
        audioSource.spatialBlend = 1f;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.dopplerLevel = 0f;
    }

    private void Update()
    {
        if (!IsMoving())
        {
            stepTimer = 0f;
            return;
        }

        float interval = GetCurrentInterval();

        stepTimer += Time.deltaTime;

        if (stepTimer >= interval)
        {
            stepTimer = 0f;
            PlayStep();
        }
    }

    private bool IsMoving()
    {
        if (controller != null)
            return controller.isGrounded && controller.velocity.magnitude > 0.1f;

        if (agent != null)
            return agent.velocity.magnitude > 0.1f;

        return false;
    }

    private float GetCurrentInterval()
    {
        // PLAYER
        if (playerMovement != null)
        {
            if (playerMovement.SprintHeld)
                return runInterval;

            if (playerMovement.CrouchHeld)
                return crouchInterval;

            return walkInterval;
        }

        // ENEMY
        if (agent != null)
        {
            if (agent.speed > 3.5f)
                return runInterval;

            return walkInterval;
        }

        return walkInterval;
    }

    private void PlayStep()
    {
        if (audioSource.clip == null)
            return;

        audioSource.PlayOneShot(audioSource.clip);
    }
}