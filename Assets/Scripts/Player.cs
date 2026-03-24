using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private FlashlightSystem flashlightSystem;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (staminaSystem == null)
            staminaSystem = GetComponent<StaminaSystem>();

        if (flashlightSystem == null)
            flashlightSystem = GetComponentInChildren<FlashlightSystem>(true);
    }

    private void OnEnable()
    {
        if (GameManager.HasInstance && movement != null)
            GameManager.Instance.RegisterPlayer(movement, staminaSystem, flashlightSystem);
    }

    private void OnDisable()
    {
        if (GameManager.HasInstance && movement != null)
            GameManager.Instance.UnregisterPlayer(movement);
    }

    public void OnMove(InputValue value)
    {
        if (movement == null) return;
        movement.MoveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (movement == null) return;
        movement.LookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (movement == null) return;
        if (value.isPressed) movement.JumpRequested = true;
    }

    public void OnCombatJump(InputValue value)
    {
        if (movement == null) return;
        if (value.isPressed) movement.CombatJumpRequested = true;
    }

    public void OnSprint(InputValue value)
    {
        if (movement == null) return;
        movement.SprintHeld = value.isPressed;
    }

    public void OnCrouch(InputValue value)
    {
        if (movement == null) return;

        movement.CrouchHeld = value.isPressed;
        if (value.isPressed)
            movement.CrouchPressedThisFrame = true;
    }

    public void OnFlashlight(InputValue value)
    {
        if (flashlightSystem == null) return;
        if (value.isPressed) flashlightSystem.Toggle();
    }
}