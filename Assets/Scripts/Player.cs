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

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        if (value.isPressed) movement.TryJump();
    }
}
