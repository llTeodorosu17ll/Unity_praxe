using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class Player_Movement_Script : MonoBehaviour
{
    public float maxSpeed = 3.5f;
    public float Acceleration = 15f;


    [SerializeField] float JumpHeight = 2f; 



    public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);

    public float PitchLimit = 85f;

    [SerializeField] float currentPitch = 0f;

    public float CurrentPitch
    {
        get => currentPitch;

        set
        {
            currentPitch = Mathf.Clamp(value, -PitchLimit, PitchLimit);
        }
    }

    [SerializeField] float GravityScale = 3f;


    public float VerticalVelocity = 0f;
    public Vector3 CurrentVelocity { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => controller.isGrounded;


    public Vector2 MoveInput;
    public Vector2 LookInput;



    [SerializeField] CinemachineCamera tpCamera;
    [SerializeField] CharacterController controller;

    #region Unity Methods

    void OnValidate()
    {
        if (controller == null)
        {
            controller= GetComponent<CharacterController>();
        }    
    }

    void Update()
    {
        MoveUpdate();
        LookUpdate();
    }

    #endregion

    #region Controller Methods

    public void TryJump()
    {
        if(IsGrounded == false)
        {
            return;
        }

        VerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y * GravityScale);
    }

    void MoveUpdate()
    {
        Vector3 motion = transform.forward * MoveInput.y + transform.right* MoveInput.x;
        motion.y = 0f;
        motion.Normalize();

        if (motion.sqrMagnitude >= 0.01f)
        {
            CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, motion * maxSpeed, Acceleration * Time.deltaTime);
        }
        else
        {
            CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, Vector3.zero, Acceleration * Time.deltaTime); 
        }


        if(IsGrounded && VerticalVelocity <= 0.01f)
        {
            VerticalVelocity = -3f;
        }
        
        VerticalVelocity += Physics.gravity.y * GravityScale * Time.deltaTime; 

        Vector3 fullVelocity = new Vector3(CurrentVelocity.x, VerticalVelocity, CurrentVelocity.z);



        controller.Move(fullVelocity * Time.deltaTime);
    }


    void LookUpdate()
    {
        Vector2 input = new Vector2(LookInput.x * LookSensitivity.x, LookInput.y * LookSensitivity.y);


        CurrentPitch -= input.y;

        tpCamera.transform.localRotation= Quaternion.Euler(CurrentPitch,0,0);


        transform.Rotate(Vector3.up * input.x);
    }



    #endregion
}
