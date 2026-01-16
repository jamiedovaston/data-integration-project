using System;
using System.Globalization;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.StandaloneInputModule;

[SelectionBase]
[RequireComponent(typeof(CharacterController))]
public class NetworkedPlayerController : NetworkBehaviour
{
    CharacterController m_Controller;
    InputSystem_Actions m_InputActions;

    IPlayerMovementable m_Movement;
    IPlayerLookable m_Look;

    [SerializeField] private Transform m_Orientation;
    [SerializeField] private CinemachineCamera m_Camera;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();

        m_InputActions = new InputSystem_Actions();

        m_Movement = GetComponent<IPlayerMovementable>();
        Debug.Assert(m_Movement != null, "Movement component is missing from player!", this);
        m_Movement.Initialise(m_Controller);

        m_Look = GetComponent<IPlayerLookable>();
        Debug.Assert(m_Look != null, "Look component is missing from player!", this);
        m_Look.Initialise(m_Orientation);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) {
            m_Camera.gameObject.SetActive(false);
            return; 
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        m_InputActions.Enable();

        m_InputActions.Player.Jump.performed += m_Movement.Handle_JumpPerformed;
        m_InputActions.Player.Jump.canceled += m_Movement.Handle_JumpCanceled;

        m_InputActions.Player.Sprint.performed += m_Movement.Handle_SprintPerformed;
        m_InputActions.Player.Sprint.canceled += m_Movement.Handle_SprintCanceled;

        m_InputActions.Player.Move.performed += m_Movement.Handle_MoveAction;
        m_InputActions.Player.Move.canceled += m_Movement.Handle_MoveAction;

        m_InputActions.Player.Look.performed += m_Look.Handle_LookAction;
        m_InputActions.Player.Look.canceled += m_Look.Handle_LookAction;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        m_InputActions.Disable();

        m_InputActions.Player.Jump.performed -= m_Movement.Handle_JumpPerformed;
        m_InputActions.Player.Jump.canceled -= m_Movement.Handle_JumpCanceled;

        m_InputActions.Player.Sprint.performed -= m_Movement.Handle_SprintPerformed;
        m_InputActions.Player.Sprint.canceled -= m_Movement.Handle_SprintCanceled;

        m_InputActions.Player.Move.performed -= m_Movement.Handle_MoveAction;
        m_InputActions.Player.Move.canceled -= m_Movement.Handle_MoveAction;

        m_InputActions.Player.Look.performed -= m_Look.Handle_LookAction;
        m_InputActions.Player.Look.canceled -= m_Look.Handle_LookAction;
    }
}
