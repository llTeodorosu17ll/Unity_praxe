// PlayerMovement.cs
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 3.5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravityScale = 3f;

    [Header("Look")]
    [SerializeField] private Vector2 lookSensitivity = new Vector2(0.1f, 0.1f);
    [SerializeField] private float pitchLimit = 85f;

    [Header("References")]
    [SerializeField] private CinemachineCamera tpCamera;
    [SerializeField] private CharacterController controller;

    public Vector2 MoveInput;
    public Vector2 LookInput;

    private float currentPitch;
    private float verticalVelocity;
    private Vector3 currentVelocity;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (tpCamera == null)
            Debug.LogWarning($"{nameof(PlayerMovement)}: tpCamera is not assigned in Inspector.");
    }

    private void OnValidate()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        MoveUpdate();
        LookUpdate();
    }

    public void TryJump()
    {
        if (controller == null) return;
        if (!controller.isGrounded) return;

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y * gravityScale);
    }

    private void MoveUpdate()
    {
        if (controller == null) return;

        Vector3 dir = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        dir.y = 0f;

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector3 targetVel = dir * maxSpeed;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVel, acceleration * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity <= 0.01f)
            verticalVelocity = -3f;

        verticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;

        Vector3 motion = new Vector3(currentVelocity.x, verticalVelocity, currentVelocity.z);
        controller.Move(motion * Time.deltaTime);
    }

    private void LookUpdate()
    {
        if (tpCamera == null) return;

        float yaw = LookInput.x * lookSensitivity.x;
        float pitch = LookInput.y * lookSensitivity.y;

        transform.Rotate(Vector3.up * yaw);

        currentPitch -= pitch;
        currentPitch = Mathf.Clamp(currentPitch, -pitchLimit, pitchLimit);

        tpCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
}
