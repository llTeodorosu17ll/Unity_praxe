using System.Collections;
using System.Collections.Generic;
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

    [Header("Jump Animation Timing Fix")]
    [Tooltip("Forces animator 'IsGrounded' false for this time after jump starts, so animation switches instantly.")]
    [SerializeField] private float ungroundedGraceTime = 0.08f;

    [Tooltip("Immediately evaluates animator after triggering jump (reduces 'jump first, anim after').")]
    [SerializeField] private bool forceAnimatorEvaluateOnJump = true;

    [Header("Combat Jump (Zone)")]
    [Tooltip("Seconds before you can use the zone jump again.")]
    [SerializeField] private float combatJumpCooldown = 1.0f;

    [Tooltip("How far up from the landing point we raycast to snap to the floor.")]
    [SerializeField] private float groundSnapRayStartHeight = 2.0f;

    [Tooltip("How far down we raycast to find the ground.")]
    [SerializeField] private float groundSnapRayDistance = 5.0f;

    [Tooltip("Small lift above ground to avoid clipping.")]
    [SerializeField] private float groundSnapOffset = 0.02f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string crouchParam = "IsCrouching";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string combatJumpTrigger = "CombatJump"; // optional
    [SerializeField] private float runSpeedForAnim = 6f;
    [SerializeField] private float animSmooth = 12f;

    [Header("Stamina")]
    [SerializeField] private StaminaSystem staminaSystem;

    private CharacterController controller;

    private float verticalSpeed;
    private float yaw;
    private float pitch;

    private bool isCrouching;
    private float animSpeed;

    private Vector3 standCenter;

    // --- inputs ---
    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }
    public bool JumpRequested { get; set; }
    public bool CombatJumpRequested { get; set; }
    public bool SprintHeld { get; set; }
    public bool CrouchHeld { get; set; }
    public bool CrouchPressedThisFrame { get; set; }

    // --- zone jump ---
    private readonly List<CombatJumpZone> zonesInside = new List<CombatJumpZone>(4);
    private bool isZoneJumping;
    private Coroutine zoneJumpRoutine;
    private float combatJumpCooldownTimer;

    // --- animation timing fix ---
    private float forceUngroundedTimer;

    // required by SaveGameManager
    public float GetYaw() => yaw;
    public float GetPitch() => pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        standCenter = controller.center;

        if (cameraTarget == null)
        {
            var t = transform.Find("CameraTarget");
            if (t != null) cameraTarget = t;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
            animator.applyRootMotion = false;

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
        if (!enabled) return;
        if (controller == null) return;

        if (combatJumpCooldownTimer > 0f)
            combatJumpCooldownTimer = Mathf.Max(0f, combatJumpCooldownTimer - Time.deltaTime);

        if (forceUngroundedTimer > 0f)
            forceUngroundedTimer = Mathf.Max(0f, forceUngroundedTimer - Time.deltaTime);

        HandleLook();

        // IMPORTANT: during loading SaveGameManager disables CharacterController.
        // Never call CharacterController.Move while it's disabled.
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
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 moveDir = transform.right * input.x + transform.forward * input.z;

        bool sprintAllowed =
            SprintHeld &&
            !isCrouching &&
            staminaSystem != null &&
            staminaSystem.CanSprint;

        float speed = moveSpeed * (sprintAllowed ? sprintMultiplier : 1f);
        speed *= (isCrouching ? crouchSpeedMultiplier : 1f);

        if (staminaSystem != null)
            staminaSystem.UpdateStamina(sprintAllowed);

        bool canJump = grounded && (!blockJumpWhileCrouched || !isCrouching);

        // ---- Combat zone jump (priority, only consumes if it triggers) ----
        if (CombatJumpRequested)
        {
            CombatJumpRequested = false;

            if (canJump && combatJumpCooldownTimer <= 0f)
            {
                var zone = ChooseBestZone();
                if (zone != null && zone.IsValid && zone.CanUseFrom(transform.position))
                {
                    var landing = zone.GetOtherSideLanding(transform.position);
                    if (landing != null)
                    {
                        combatJumpCooldownTimer = Mathf.Max(0f, combatJumpCooldown);
                        forceUngroundedTimer = Mathf.Max(forceUngroundedTimer, ungroundedGraceTime);

                        StartZoneJump(zone, landing);
                        return;
                    }
                }
            }
        }

        // ---- Normal jump ----
        if (JumpRequested)
        {
            JumpRequested = false;

            if (canJump)
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

                // Force "airborne" for animator immediately
                forceUngroundedTimer = Mathf.Max(forceUngroundedTimer, ungroundedGraceTime);

                TriggerJumpAnimImmediate(jumpTrigger);
            }
        }

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * speed;
        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);
    }

    private void TriggerJumpAnimImmediate(string trigger)
    {
        if (animator == null) return;

        // Make animator see airborne right now
        if (!string.IsNullOrEmpty(groundedParam))
            animator.SetBool(groundedParam, false);

        if (!string.IsNullOrEmpty(trigger))
            animator.SetTrigger(trigger);

        // This removes the “one-frame late” feeling
        if (forceAnimatorEvaluateOnJump)
            animator.Update(0f);
    }

    private CombatJumpZone ChooseBestZone()
    {
        if (zonesInside.Count == 0) return null;

        CombatJumpZone best = null;
        float bestD = float.MaxValue;

        Vector3 p = transform.position;
        p.y = 0f;

        for (int i = zonesInside.Count - 1; i >= 0; i--)
        {
            var z = zonesInside[i];
            if (z == null)
            {
                zonesInside.RemoveAt(i);
                continue;
            }

            Vector3 zp = z.transform.position;
            zp.y = 0f;

            float d = (p - zp).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = z;
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

        // Face toward landing
        Vector3 dir = end - start;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            yaw = transform.eulerAngles.y;
        }

        // Animation: CombatJump if exists, else Jump
        if (animator != null)
        {
            if (HasTrigger(animator, combatJumpTrigger))
                TriggerJumpAnimImmediate(combatJumpTrigger);
            else
                TriggerJumpAnimImmediate(jumpTrigger);
        }

        float duration = Mathf.Max(0.05f, zone.TravelTime);
        float arc = Mathf.Max(0f, zone.ArcHeight);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float tt = Mathf.Clamp01(t);

            Vector3 target = Vector3.Lerp(start, end, tt);
            target.y += Mathf.Sin(tt * Mathf.PI) * arc;

            // If controller becomes disabled (load), abort safely
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

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Mathf.Max(0.2f, groundSnapRayDistance), ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y + groundSnapOffset;
            transform.position = p;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // If controller disabled (load), treat as grounded=false to avoid weird transitions
        bool controllerActive = controller.enabled;

        bool groundedForAnim =
            controllerActive &&
            controller.isGrounded &&
            forceUngroundedTimer <= 0f &&
            !isZoneJumping;

        animator.SetBool(groundedParam, groundedForAnim);
        animator.SetBool(crouchParam, isCrouching);

        float horizontalSpeed = 0f;

        if (controllerActive && !isZoneJumping)
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            horizontalSpeed = v.magnitude;
        }

        float normalized = Mathf.Clamp01(horizontalSpeed / Mathf.Max(0.1f, runSpeedForAnim));
        animSpeed = Mathf.Lerp(animSpeed, normalized, 1f - Mathf.Exp(-animSmooth * Time.deltaTime));

        animator.SetFloat(speedParam, animSpeed);
    }

    private bool HasTrigger(Animator a, string triggerName)
    {
        if (a == null || string.IsNullOrEmpty(triggerName)) return false;
        var ps = a.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].type == AnimatorControllerParameterType.Trigger && ps[i].name == triggerName)
                return true;
        }
        return false;
    }

    public void RegisterCombatJumpZone(CombatJumpZone zone)
    {
        if (zone == null) return;
        if (!zonesInside.Contains(zone))
            zonesInside.Add(zone);
    }

    public void UnregisterCombatJumpZone(CombatJumpZone zone)
    {
        if (zone == null) return;
        zonesInside.Remove(zone);
    }

    // required by SaveGameManager
    public void SetLookRotation(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}