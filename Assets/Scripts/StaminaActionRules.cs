using UnityEngine;

[DefaultExecutionOrder(-200)]
public class StaminaActionRules : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private StaminaSystem stamina;
    [SerializeField] private CharacterController characterController;

    [Header("Rules")]
    [SerializeField] private float minPercentToStartSprint = 10f;
    [SerializeField] private float minPercentToJump = 10f;

    [Tooltip("How much stamina is spent per jump (percent of max).")]
    [SerializeField] private float jumpCostPercent = 10f;

    [Tooltip("Also blocks/spends stamina for CombatJumpRequested if you use it.")]
    [SerializeField] private bool applyToCombatJump = true;

    private bool prevJumpReq;
    private bool prevCombatJumpReq;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (stamina == null)
            stamina = GetComponent<StaminaSystem>();

        if (stamina == null)
            stamina = FindFirstObjectByType<StaminaSystem>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (movement == null || stamina == null)
            return;

        // Block sprint if stamina too low
        if (movement.SprintHeld && !stamina.HasAtLeastPercent(minPercentToStartSprint))
            movement.SprintHeld = false;

        bool grounded = characterController != null && characterController.enabled && characterController.isGrounded;

        // Normal jump: ONLY spend if grounded (so no stamina loss in air)
        bool jumpRising = movement.JumpRequested && !prevJumpReq;
        if (jumpRising)
        {
            if (!grounded)
            {
                // ignore jump press in air (no stamina cost)
                movement.JumpRequested = false;
            }
            else if (!stamina.HasAtLeastPercent(minPercentToJump))
            {
                movement.JumpRequested = false;
            }
            else
            {
                if (!stamina.TrySpendPercent(jumpCostPercent))
                    movement.JumpRequested = false;
            }
        }

        // Combat jump: same rule
        if (applyToCombatJump)
        {
            bool combatRising = movement.CombatJumpRequested && !prevCombatJumpReq;
            if (combatRising)
            {
                if (!grounded)
                {
                    movement.CombatJumpRequested = false;
                }
                else if (!stamina.HasAtLeastPercent(minPercentToJump))
                {
                    movement.CombatJumpRequested = false;
                }
                else
                {
                    if (!stamina.TrySpendPercent(jumpCostPercent))
                        movement.CombatJumpRequested = false;
                }
            }
        }

        prevJumpReq = movement.JumpRequested;
        prevCombatJumpReq = movement.CombatJumpRequested;
    }
}