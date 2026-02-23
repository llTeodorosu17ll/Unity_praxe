using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [SerializeField] private float volume = 1f;

    [Header("Pitch Variation")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Movement Check")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private float minVelocityToPlay = 0.1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (navMeshAgent == null)
            navMeshAgent = GetComponentInParent<NavMeshAgent>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; // FORCE 3D
    }

    // Called from Animation Event
    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return;

        if (!IsMoving())
            return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
    }

    private bool IsMoving()
    {
        if (characterController != null)
        {
            if (!characterController.isGrounded)
                return false;

            return characterController.velocity.magnitude > minVelocityToPlay;
        }

        if (navMeshAgent != null)
        {
            return navMeshAgent.velocity.magnitude > minVelocityToPlay;
        }

        return true;
    }
}
