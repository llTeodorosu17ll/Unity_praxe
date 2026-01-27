using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
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

    public void OnJump()
    {
        if (movement == null) return;
        movement.JumpRequested = true;
    }

    public void OnSprint(InputValue value)
    {
        if (movement == null) return;
        movement.SprintHeld = value.isPressed;
    }
}
