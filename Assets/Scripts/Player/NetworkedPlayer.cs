using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerable
{
    NetworkObject NetObject { get; }

    IPlayerHealthable m_Health { get; set; }
    void TeleportRpc(Vector3 pos, Quaternion rot);
    void LockMovementRpc(bool locked);
}


[SelectionBase]
[RequireComponent(typeof(CharacterController))]
public class NetworkedPlayer : NetworkBehaviour, IPlayerable
{
    CharacterController m_Controller;
    InputSystem_Actions m_InputActions;

    IPlayerMovementable m_Movement;
    IPlayerLookable m_Look;
    IPlayerShootable m_Shoot;
    public IPlayerHealthable m_Health { get; set; }

    public NetworkObject NetObject => NetworkObject;

    [SerializeField] private Transform m_Orientation;
    [SerializeField] private CinemachineCamera m_Camera;

    public static Action<InputAction.CallbackContext> OnPlayerEscapePressed;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();

        m_InputActions = new InputSystem_Actions();

        m_Movement = GetComponent<IPlayerMovementable>();
        Debug.Assert(m_Movement != null, "Movement component is missing from player!", this);
        m_Movement.Initialise(m_Controller);

        m_Look = GetComponent<IPlayerLookable>();
        Debug.Assert(m_Look != null, "Look component is missing from player!", this);
        m_Look.Initialise(m_Orientation, m_Camera);

        m_Shoot = GetComponent<IPlayerShootable>();
        Debug.Assert(m_Shoot != null, "Shoot component is missing from player!", this);
        m_Shoot.Initialise(m_Camera);

        m_Health = GetComponent<IPlayerHealthable>();
        Debug.Assert(m_Health != null, "Health component is missing from player!", this);
        m_Health.SetEnabled(false);
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

        m_InputActions.Player.LeanR.performed += m_Look.Handle_LeanRPerformed;
        m_InputActions.Player.LeanL.performed += m_Look.Handle_LeanLPerformed;

        m_InputActions.Player.Attack.performed += m_Shoot.Handle_ShootPerformed;
        m_InputActions.Player.Attack.canceled += m_Shoot.Handle_ShootCanceled;

        m_InputActions.Player.Aim.performed += m_Shoot.Handle_AimPerformed;
        m_InputActions.Player.Aim.canceled += m_Shoot.Handle_AimCanceled;

        m_InputActions.Player.Crouch.performed += m_Movement.Handle_SlidePerformed;
        m_InputActions.Player.Crouch.canceled += m_Movement.Handle_SlideCanceled;

        m_InputActions.System.Escape.performed += HandleEscape;
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

        m_InputActions.Player.LeanR.performed -= m_Look.Handle_LeanRPerformed;
        m_InputActions.Player.LeanL.performed -= m_Look.Handle_LeanLPerformed;

        m_InputActions.Player.Attack.performed -= m_Shoot.Handle_ShootPerformed;
        m_InputActions.Player.Attack.canceled -= m_Shoot.Handle_ShootCanceled;

        m_InputActions.Player.Aim.performed -= m_Shoot.Handle_AimPerformed;
        m_InputActions.Player.Aim.canceled -= m_Shoot.Handle_AimCanceled;

        m_InputActions.Player.Crouch.performed -= m_Movement.Handle_SlidePerformed;
        m_InputActions.Player.Crouch.canceled -= m_Movement.Handle_SlideCanceled;

        m_InputActions.System.Escape.performed -= HandleEscape;
    }

    private void HandleEscape(InputAction.CallbackContext ctx) => NetworkManager.Shutdown();

    [Rpc(SendTo.Owner)]
    public void TeleportRpc(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
    }

    [Rpc(SendTo.Owner)]
    public void LockMovementRpc(bool locked)
    {
        m_Movement.CanMove = !locked;
    }

}
