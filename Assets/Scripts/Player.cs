using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player_Movement_Script))]
public class Player : MonoBehaviour
{
    [SerializeField] Player_Movement_Script Player_Movement_Script;

    #region Input Handling

    void OnMove(InputValue value)
    { 
        Player_Movement_Script.MoveInput = value.Get< Vector2 > ();
    }

    void OnLook(InputValue value)
    {
        Player_Movement_Script.LookInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Player_Movement_Script.TryJump();
        }
    }

    #endregion

    #region Unity Methods

    void OnValidate()
    {
        if (Player_Movement_Script == null) Player_Movement_Script= GetComponent<Player_Movement_Script>();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    #endregion
}
