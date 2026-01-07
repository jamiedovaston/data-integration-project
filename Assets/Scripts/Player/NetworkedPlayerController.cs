using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[SelectionBase]
[RequireComponent(typeof(CharacterController))]
public class NetworkedPlayerController : NetworkBehaviour
{
    CharacterController m_Controller;
    InputSystem_Actions m_InputActions;

    Vector2 Input_Move = Vector2.zero;
    Vector2 Input_Look = Vector2.zero;
    private bool Input_JumpingHeld = false;

    [Header("Movement")]
    [SerializeField] private float speed = 8.0f;

    [Header("Jumping")]
    [SerializeField] private float gravity = 40.1f;
    [SerializeField] private float jumpSpeed = 8.0f;

    [Header("Look")]
    [SerializeField] private float lookSpeed = 2.0f;
    [SerializeField] private float lookXLimit = 90.0f;

    Vector3 moveDirection = Vector3.zero;

    private float rotationX;
    [SerializeField] private GameObject orientation;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();

        m_InputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        m_InputActions.Enable();

        m_InputActions.Player.Jump.performed += Handle_JumpPerformed;
        m_InputActions.Player.Jump.canceled += Handle_JumpCanceled;

        m_InputActions.Player.Move.performed += Handle_MoveAction;
        m_InputActions.Player.Move.canceled += Handle_MoveAction;

        m_InputActions.Player.Look.performed += Handle_LookAction;
        m_InputActions.Player.Look.canceled += Handle_LookAction;
    }

    private void OnDisable()
    {
        m_InputActions.Disable();

        m_InputActions.Player.Jump.performed -= Handle_JumpPerformed;
        m_InputActions.Player.Jump.canceled -= Handle_JumpCanceled;

        m_InputActions.Player.Move.performed -= Handle_MoveAction;
        m_InputActions.Player.Move.canceled -= Handle_MoveAction;

        m_InputActions.Player.Look.performed -= Handle_LookAction;
        m_InputActions.Player.Look.canceled -= Handle_LookAction;
    }

    private void FixedUpdate()
    {
        Movement();
        Look();
    }

    private void Look()
    {
        rotationX += -Input_Look.y * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        orientation.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input_Look.x * lookSpeed, 0);
    }

    private void Movement()
    {
        Vector3 direcion = (transform.forward * Input_Move.y) + (transform.right * Input_Move.x);
        float moveDirectionY = moveDirection.y;
        moveDirection = direcion * speed * Time.fixedDeltaTime;
        moveDirection.y = moveDirectionY;

        // Gravity
        if (!m_Controller.isGrounded)
        {
            moveDirection.y -= gravity * Time.fixedDeltaTime;
        }

        if (Input_JumpingHeld && m_Controller.isGrounded)
        {
            moveDirection.y = jumpSpeed * Time.fixedDeltaTime;
        }

        m_Controller.Move(moveDirection);
    }

    private void Handle_MoveAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        Input_Move = context.ReadValue<Vector2>();
    }

    private void Handle_LookAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        Input_Look = context.ReadValue<Vector2>();
    }

    private void Handle_JumpPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        Input_JumpingHeld = true;
    }

    private void Handle_JumpCanceled(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        Input_JumpingHeld = false;
    }
}
