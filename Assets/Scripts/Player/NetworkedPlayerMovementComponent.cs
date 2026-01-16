using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerMovementable
{
    void Initialise(CharacterController characterController);
    void Handle_MoveAction(InputAction.CallbackContext context);
    void Handle_JumpPerformed(InputAction.CallbackContext context);
    void Handle_JumpCanceled(InputAction.CallbackContext context);
    void Handle_SprintPerformed(InputAction.CallbackContext context);
    void Handle_SprintCanceled(InputAction.CallbackContext context);
}

public class NetworkedPlayerMovementComponent : NetworkBehaviour, IPlayerMovementable
{
    CharacterController m_Controller;

    Vector2 Input_Move = Vector2.zero;
    bool Input_JumpingHeld = false;
    bool Input_SprintingHeld = false;

    bool isSprinting = false;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float sprintSpeed = 14f;
    [SerializeField] private float acceleration = 12f;

    [Header("Jumping")]
    [SerializeField] private float gravity = 40.1f;
    [SerializeField] private float jumpSpeed = 8f;

    Vector3 moveDirection = Vector3.zero;
    Vector3 currentVelocity = Vector3.zero;

    void IPlayerMovementable.Initialise(CharacterController controller)
    {
        m_Controller = controller;
    }

    private void Update()
    {
        UpdateSprintState();
        Movement();
    }

    private void UpdateSprintState()
    {
        if (m_Controller.isGrounded)
        {
            if (Input_SprintingHeld && Input_Move.y > 0.1f)
                isSprinting = true;
            else
                isSprinting = false;
        }
    }

    private void Movement()
    {
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Direction multipliers
        float forwardMultiplier = 1.0f;
        float backwardMultiplier = 0.9f;
        float strafeMultiplier = 0.75f;

        // Input split
        float fwd = Mathf.Clamp(Input_Move.y, 0f, 1f) * forwardMultiplier;
        float back = Mathf.Abs(Mathf.Clamp(Input_Move.y, -1f, 0f)) * backwardMultiplier;
        float side = Mathf.Abs(Input_Move.x) * strafeMultiplier;

        // Combine final directional multipliers
        Vector3 desired =
            transform.forward * (fwd - back) +
            transform.right * (Input_Move.x * strafeMultiplier);

        // Apply speed
        desired *= targetSpeed;

        // Smooth accel/decel
        currentVelocity = Vector3.Lerp(currentVelocity, desired, acceleration * Time.deltaTime);

        // Preserve vertical velocity
        float verticalVel = moveDirection.y;
        moveDirection = currentVelocity;
        moveDirection.y = verticalVel;

        // gravity + jump handling
        if (m_Controller.isGrounded)
        {
            if (Input_JumpingHeld)
                moveDirection.y = jumpSpeed;
            else
                moveDirection.y = -1f;
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        m_Controller.Move(moveDirection * Time.deltaTime);
    }


    void IPlayerMovementable.Handle_MoveAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_Move = context.ReadValue<Vector2>();
    }

    void IPlayerMovementable.Handle_JumpPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_JumpingHeld = true;
    }

    void IPlayerMovementable.Handle_JumpCanceled(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_JumpingHeld = false;
    }

    void IPlayerMovementable.Handle_SprintPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_SprintingHeld = true;
    }

    void IPlayerMovementable.Handle_SprintCanceled(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_SprintingHeld = false;
    }
}
