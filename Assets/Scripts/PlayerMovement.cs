using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private const float UngroundedGraceTime = 0.08f;
    private const bool ForceAnimatorEvaluateOnJump = true;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int CrouchHash = Animator.StringToHash("IsCrouching");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int CombatJumpHash = Animator.StringToHash("CombatJump");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;

    [Header("Crouch")]
    [SerializeField] private bool crouchToggle = true;
    [SerializeField] private float crouchSpeedMultiplier = 0.45f;
    [SerializeField] private bool blockJumpWhileCrouched = true;

    [Header("Character Controller")]
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchCenterYOffset = 0f;

    [Header("Look")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float yawSensitivity = 0.12f;
    [SerializeField] private float pitchSensitivity = 0.10f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Combat Jump")]
    [SerializeField] private float combatJumpCooldown = 1.0f;
    [SerializeField] private float groundSnapRayStartHeight = 2.0f;
    [SerializeField] private float groundSnapRayDistance = 5.0f;
    [SerializeField] private float groundSnapOffset = 0.02f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float runSpeedForAnim = 6f;
    [SerializeField] private float animSmooth = 12f;

    private CharacterController controller;
    private StaminaSystem staminaSystem;

    private float verticalSpeed;
    private float yaw;
    private float pitch;
    private float animSpeed;

    private bool isCrouching;
    private bool isZoneJumping;
    private bool hasCombatJumpTrigger;

    private float combatJumpCooldownTimer;
    private float forceUngroundedTimer;

    private Vector3 standCenter;

    private Coroutine zoneJumpRoutine;
    private readonly List<CombatJumpZone> zonesInside = new List<CombatJumpZone>(4);

    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }
    public bool JumpRequested { get; set; }
    public bool CombatJumpRequested { get; set; }
    public bool SprintHeld { get; set; }
    public bool CrouchHeld { get; set; }
    public bool CrouchPressedThisFrame { get; set; }

    public float GetYaw() => yaw;
    public float GetPitch() => pitch;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
        staminaSystem = GetComponent<StaminaSystem>();

        if (cameraTarget == null)
        {
            Transform t = transform.Find("CameraTarget");
            if (t != null)
                cameraTarget = t;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        staminaSystem = GetComponent<StaminaSystem>();

        if (cameraTarget == null)
            Debug.LogError("PlayerMovement: cameraTarget is not assigned.", this);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.applyRootMotion = false;
            hasCombatJumpTrigger = HasTrigger(animator, CombatJumpHash);
        }

        standCenter = controller.center;
        yaw = transform.eulerAngles.y;

        pitch = 0f;
        if (cameraTarget != null)
        {
            float x = cameraTarget.localEulerAngles.x;
            if (x > 180f) x -= 360f;
            pitch = x;
        }

        ApplyControllerShapeStanding();
    }

    private void Update()
    {
        if (!enabled || controller == null)
            return;

        if (combatJumpCooldownTimer > 0f)
            combatJumpCooldownTimer = Mathf.Max(0f, combatJumpCooldownTimer - Time.deltaTime);

        if (forceUngroundedTimer > 0f)
            forceUngroundedTimer = Mathf.Max(0f, forceUngroundedTimer - Time.deltaTime);

        HandleLook();

        if (controller.enabled && !isZoneJumping)
        {
            HandleCrouch();
            HandleMoveAndJump();
        }

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

                if (isCrouching)
                    ApplyControllerShapeCrouching();
                else
                    ApplyControllerShapeStanding();
            }
        }
        else
        {
            bool wantCrouch = CrouchHeld;
            if (wantCrouch != isCrouching)
            {
                isCrouching = wantCrouch;

                if (isCrouching)
                    ApplyControllerShapeCrouching();
                else
                    ApplyControllerShapeStanding();
            }
        }
    }

    private void ApplyControllerShapeStanding()
    {
        controller.height = Mathf.Max(0.5f, standHeight);
        controller.center = standCenter;
    }

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
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 moveDir = transform.right * input.x + transform.forward * input.z;

        bool sprintAllowed =
            SprintHeld &&
            !isCrouching &&
            staminaSystem != null &&
            staminaSystem.CanSprint;

        float speed = moveSpeed * (sprintAllowed ? sprintMultiplier : 1f);
        speed *= isCrouching ? crouchSpeedMultiplier : 1f;

        if (staminaSystem != null)
            staminaSystem.UpdateStamina(sprintAllowed);

        bool canJump = grounded && (!blockJumpWhileCrouched || !isCrouching);

        if (CombatJumpRequested)
        {
            CombatJumpRequested = false;

            if (canJump && combatJumpCooldownTimer <= 0f)
            {
                CombatJumpZone zone = ChooseBestZone();
                if (zone != null && zone.IsValid && zone.CanUseFrom(transform.position))
                {
                    Transform landing = zone.GetOtherSideLanding(transform.position);
                    if (landing != null)
                    {
                        combatJumpCooldownTimer = Mathf.Max(0f, combatJumpCooldown);
                        forceUngroundedTimer = Mathf.Max(forceUngroundedTimer, UngroundedGraceTime);

                        StartZoneJump(zone, landing);
                        return;
                    }
                }
            }
        }

        if (JumpRequested)
        {
            JumpRequested = false;

            if (canJump)
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
                forceUngroundedTimer = Mathf.Max(forceUngroundedTimer, UngroundedGraceTime);
                TriggerJumpAnimImmediate(false);
            }
        }

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * speed;
        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);
    }

    private void TriggerJumpAnimImmediate(bool combatJump)
    {
        if (animator == null)
            return;

        animator.SetBool(GroundedHash, false);

        if (combatJump && hasCombatJumpTrigger)
            animator.SetTrigger(CombatJumpHash);
        else
            animator.SetTrigger(JumpHash);

        if (ForceAnimatorEvaluateOnJump)
            animator.Update(0f);
    }

    private CombatJumpZone ChooseBestZone()
    {
        if (zonesInside.Count == 0)
            return null;

        CombatJumpZone best = null;
        float bestDistance = float.MaxValue;

        Vector3 p = transform.position;
        p.y = 0f;

        for (int i = zonesInside.Count - 1; i >= 0; i--)
        {
            CombatJumpZone zone = zonesInside[i];
            if (zone == null)
            {
                zonesInside.RemoveAt(i);
                continue;
            }

            Vector3 zp = zone.transform.position;
            zp.y = 0f;

            float distance = (p - zp).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = zone;
            }
        }

        return best;
    }

    private void StartZoneJump(CombatJumpZone zone, Transform landing)
    {
        if (zoneJumpRoutine != null)
            StopCoroutine(zoneJumpRoutine);

        zoneJumpRoutine = StartCoroutine(ZoneJumpRoutine(zone, landing));
    }

    private IEnumerator ZoneJumpRoutine(CombatJumpZone zone, Transform landing)
    {
        isZoneJumping = true;

        MoveInput = Vector2.zero;
        JumpRequested = false;
        SprintHeld = false;

        Vector3 start = transform.position;
        Vector3 end = landing.position;

        Vector3 dir = end - start;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            yaw = transform.eulerAngles.y;
        }

        TriggerJumpAnimImmediate(true);

        float duration = Mathf.Max(0.05f, zone.TravelTime);
        float arc = Mathf.Max(0f, zone.ArcHeight);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float tt = Mathf.Clamp01(t);

            Vector3 target = Vector3.Lerp(start, end, tt);
            target.y += Mathf.Sin(tt * Mathf.PI) * arc;

            if (!controller.enabled)
                break;

            Vector3 delta = target - transform.position;
            controller.Move(delta);

            yield return null;
        }

        if (controller.enabled)
        {
            SnapToGround();
            verticalSpeed = -1f;
        }

        isZoneJumping = false;
        zoneJumpRoutine = null;
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(0.1f, groundSnapRayStartHeight);

        if (Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                Mathf.Max(0.2f, groundSnapRayDistance),
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y + groundSnapOffset;
            transform.position = p;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        bool controllerActive = controller.enabled;

        bool groundedForAnim =
            controllerActive &&
            controller.isGrounded &&
            forceUngroundedTimer <= 0f &&
            !isZoneJumping;

        animator.SetBool(GroundedHash, groundedForAnim);
        animator.SetBool(CrouchHash, isCrouching);

        float horizontalSpeed = 0f;

        if (controllerActive && !isZoneJumping)
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            horizontalSpeed = v.magnitude;
        }

        float normalized = Mathf.Clamp01(horizontalSpeed / Mathf.Max(0.1f, runSpeedForAnim));
        animSpeed = Mathf.Lerp(animSpeed, normalized, 1f - Mathf.Exp(-animSmooth * Time.deltaTime));

        animator.SetFloat(SpeedHash, animSpeed);
    }

    private bool HasTrigger(Animator a, int triggerHash)
    {
        if (a == null)
            return false;

        AnimatorControllerParameter[] parameters = a.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger &&
                parameters[i].nameHash == triggerHash)
            {
                return true;
            }
        }

        return false;
    }

    public void RegisterCombatJumpZone(CombatJumpZone zone)
    {
        if (zone == null)
            return;

        if (!zonesInside.Contains(zone))
            zonesInside.Add(zone);
    }

    public void UnregisterCombatJumpZone(CombatJumpZone zone)
    {
        if (zone == null)
            return;

        zonesInside.Remove(zone);
    }

    public void SetLookRotation(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}