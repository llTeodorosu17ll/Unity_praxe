using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;

    [Header("Crouch")]
    [SerializeField] private bool crouchToggle = true;
    [SerializeField] private float crouchSpeedMultiplier = 0.45f;
    [SerializeField] private bool blockJumpWhileCrouched = true;

    [Header("CharacterController")]
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.1f;

    [Tooltip("Applied ONLY while crouching: centerY = height/2 + offset")]
    [SerializeField] private float crouchCenterYOffset = 0f;

    [Header("Look")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float yawSensitivity = 0.12f;
    [SerializeField] private float pitchSensitivity = 0.10f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Jump/Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string crouchParam = "IsCrouching";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private float runSpeedForAnim = 6f;
    [SerializeField] private float animSmooth = 12f;

    private CharacterController controller;

    private float verticalSpeed;
    private float yaw;
    private float pitch;

    private bool isCrouching;
    private float animSpeed;

    // store the original center set in Inspector (standing)
    private Vector3 standCenter;

    // Input from Player.cs
    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }
    public bool JumpRequested { get; set; }
    public bool SprintHeld { get; set; }

    // Crouch input from Player.cs (Input System)
    public bool CrouchHeld { get; set; }
    public bool CrouchPressedThisFrame { get; set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // remember standing center (whatever you set in Inspector)
        standCenter = controller.center;

        if (cameraTarget == null)
        {
            var t = transform.Find("CameraTarget");
            if (t != null) cameraTarget = t;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        yaw = transform.eulerAngles.y;

        pitch = 0f;
        if (cameraTarget != null)
        {
            float x = cameraTarget.localEulerAngles.x;
            if (x > 180f) x -= 360f;
            pitch = x;
        }

        // standing shape: only height changes, center stays what you set
        ApplyControllerShapeStanding();
    }

    private void Update()
    {
        HandleLook();
        HandleCrouch();
        HandleMoveAndJump();
        UpdateAnimator();

        CrouchPressedThisFrame = false;
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

    private void HandleCrouch()
    {
        if (crouchToggle)
        {
            if (CrouchPressedThisFrame)
            {
                isCrouching = !isCrouching;

                if (isCrouching) ApplyControllerShapeCrouching();
                else ApplyControllerShapeStanding();
            }
        }
        else
        {
            bool wantCrouch = CrouchHeld;
            if (wantCrouch != isCrouching)
            {
                isCrouching = wantCrouch;

                if (isCrouching) ApplyControllerShapeCrouching();
                else ApplyControllerShapeStanding();
            }
        }
    }

    // Standing: height = standHeight, center stays EXACTLY what you set in Inspector
    private void ApplyControllerShapeStanding()
    {
        controller.height = Mathf.Max(0.5f, standHeight);
        controller.center = standCenter;
    }

    // Crouching: height = crouchHeight AND center.y is forced to height/2 (only here)
    private void ApplyControllerShapeCrouching()
    {
        controller.height = Mathf.Max(0.5f, crouchHeight);

        Vector3 c = controller.center;
        c.y = (controller.height * 0.5f) + crouchCenterYOffset;
        controller.center = c;
    }

    private void HandleMoveAndJump()
    {
        bool grounded = controller.isGrounded;

        if (grounded && verticalSpeed < 0f)
            verticalSpeed = -1f;

        Vector3 input = new Vector3(MoveInput.x, 0f, MoveInput.y);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 moveDir = transform.right * input.x + transform.forward * input.z;

        bool sprintAllowed = SprintHeld && !isCrouching;
        float speed = moveSpeed * (sprintAllowed ? sprintMultiplier : 1f);
        speed *= (isCrouching ? crouchSpeedMultiplier : 1f);

        bool canJump = grounded && (!blockJumpWhileCrouched || !isCrouching);

        if (JumpRequested)
        {
            JumpRequested = false;

            if (canJump)
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetTrigger(jumpTrigger);
            }
        }

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * speed;
        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(groundedParam, controller.isGrounded);
        animator.SetBool(crouchParam, isCrouching);

        Vector3 v = controller.velocity;
        v.y = 0f;
        float horizontalSpeed = v.magnitude;

        float normalized = Mathf.Clamp01(horizontalSpeed / Mathf.Max(0.1f, runSpeedForAnim));
        animSpeed = Mathf.Lerp(animSpeed, normalized, 1f - Mathf.Exp(-animSmooth * Time.deltaTime));

        animator.SetFloat(speedParam, animSpeed);
    }
}
