using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;

    [Header("Look")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float yawSensitivity = 0.12f;
    [SerializeField] private float pitchSensitivity = 0.10f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Jump/Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float verticalSpeed;
    private float yaw;
    private float pitch;

    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }
    public bool JumpRequested { get; set; }
    public bool SprintHeld { get; set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTarget == null)
        {
            var t = transform.Find("CameraTarget");
            if (t != null) cameraTarget = t;
        }

        yaw = transform.eulerAngles.y;

        pitch = 0f;
        if (cameraTarget != null)
        {
            float x = cameraTarget.localEulerAngles.x;
            if (x > 180f) x -= 360f;
            pitch = x;
        }
    }

    private void Update()
    {
        HandleLook();
        HandleMoveAndJump();
    }

    private void HandleLook()
    {
        yaw += LookInput.x * yawSensitivity;
        pitch -= LookInput.y * pitchSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMoveAndJump()
    {
        bool grounded = controller.isGrounded;
        if (grounded && verticalSpeed < 0f) verticalSpeed = -2f;

        Vector3 input = new Vector3(MoveInput.x, 0f, MoveInput.y);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 moveDir = transform.right * input.x + transform.forward * input.z;

        if (JumpRequested)
        {
            JumpRequested = false;
            if (grounded)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalSpeed += gravity * Time.deltaTime;

        float speed = moveSpeed * (SprintHeld ? sprintMultiplier : 1f);

        Vector3 velocity = moveDir * speed;
        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);
    }
}
