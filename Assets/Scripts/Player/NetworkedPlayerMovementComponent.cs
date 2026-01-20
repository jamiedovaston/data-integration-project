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
    void Handle_SlidePerformed(InputAction.CallbackContext context);
    void Handle_SlideCanceled(InputAction.CallbackContext context);
}

public class NetworkedPlayerMovementComponent : NetworkBehaviour, IPlayerMovementable
{
    CharacterController m_Controller;

    Vector2 Input_Move = Vector2.zero;
    bool Input_JumpingHeld = false;
    bool Input_SprintingHeld = false;
    bool Input_SlideHeld = false;

    bool wasSlideHeldLastFrame = false;

    bool isSprinting = false;
    bool isCrouching = false;
    bool isSliding = false;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float sprintSpeed = 14f;
    [SerializeField] private float acceleration = 12f;

    [Header("Jumping")]
    [SerializeField] private float gravity = 40.1f;
    [SerializeField] private float jumpSpeed = 8f;

    [Header("Crouch & Slide")]
    [SerializeField] private float normalHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeed = 4f;

    [SerializeField] private float slideDuration = 0.65f;
    [SerializeField] private float slideSpeed = 11f;
    [SerializeField] private float slideCooldown = 1.0f;
    [SerializeField] private float slideControlReduction = 0.6f;

    float slideTimer = 0f;
    float slideCooldownTimer = 0f;

    [Header("Animation")]
    [SerializeField] private Animator m_Animator;

    Vector3 moveDirection = Vector3.zero;
    Vector3 currentVelocity = Vector3.zero;

    public void Initialise(CharacterController controller)
    {
        m_Controller = controller;
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateSprintState();
        UpdateSlideCrouchState();
        Movement();
    }

    private void UpdateSlideCrouchState()
    {
        if (slideCooldownTimer > 0)
            slideCooldownTimer -= Time.deltaTime;

        bool slidePressed = Input_SlideHeld && !wasSlideHeldLastFrame;
        wasSlideHeldLastFrame = Input_SlideHeld;

        if (!isSliding && slidePressed && isSprinting && slideCooldownTimer <= 0)
        {
            StartSlide();
            return;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            if (slideTimer <= 0 || !Input_SlideHeld)
            {
                EndSlideIntoCrouch();
            }

            return;
        }

        if (Input_SlideHeld && !isCrouching)
            StartCrouch();

        if (!Input_SlideHeld && isCrouching)
            EndCrouch();
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;

        m_Controller.height = crouchHeight;

        m_Animator.SetBool("isSliding", true);
    }

    private void EndSlideIntoCrouch()
    {
        isSliding = false;
        isCrouching = true;

        m_Animator.SetBool("isSliding", false);
        m_Animator.SetBool("isCrouching", true);

        m_Controller.height = crouchHeight;
    }

    private void StartCrouch()
    {
        isCrouching = true;
        m_Controller.height = crouchHeight;

        m_Animator.SetBool("isCrouching", true);
    }

    private void EndCrouch()
    {
        isCrouching = false;
        m_Controller.height = normalHeight;

        m_Animator.SetBool("isCrouching", false);
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
        Vector3 desired;

        if (isSliding)
        {
            desired = transform.forward * slideSpeed;
            desired += transform.right * (Input_Move.x * walkSpeed * slideControlReduction);
        }
        else
        {
            float targetSpeed;

            if (isSliding)
            {
                targetSpeed = slideSpeed;
            }
            else if (isCrouching)
            {
                targetSpeed = crouchSpeed;
            }
            else
            {
                targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            }


            float forwardMultiplier = 1.0f;
            float backwardMultiplier = 0.9f;
            float strafeMultiplier = 0.75f;

            float fwd = Mathf.Clamp(Input_Move.y, 0f, 1f) * forwardMultiplier;
            float back = Mathf.Abs(Mathf.Clamp(Input_Move.y, -1f, 0f)) * backwardMultiplier;

            desired =
                transform.forward * (fwd - back) +
                transform.right * (Input_Move.x * strafeMultiplier);

            desired *= targetSpeed;
        }

        currentVelocity = Vector3.Lerp(currentVelocity, desired, acceleration * Time.deltaTime);

        float verticalVel = moveDirection.y;
        moveDirection = currentVelocity;
        moveDirection.y = verticalVel;

        m_Animator.SetBool("isGrounded", m_Controller.isGrounded);

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

        Vector3 localVel = transform.InverseTransformDirection(currentVelocity);
        float blendX = Mathf.Clamp(localVel.x / walkSpeed, -2f, 2f);
        float blendY = Mathf.Clamp(localVel.z / walkSpeed, -2f, 2f);

        m_Animator.SetFloat("InpX", blendX);
        m_Animator.SetFloat("InpY", blendY);
    }

    public void Handle_MoveAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_Move = context.ReadValue<Vector2>();
    }

    public void Handle_JumpPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_JumpingHeld = true;
    }

    public void Handle_JumpCanceled(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_JumpingHeld = false;
    }

    public void Handle_SprintPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_SprintingHeld = true;
    }

    public void Handle_SprintCanceled(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_SprintingHeld = false;
    }

    public void Handle_SlidePerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_SlideHeld = true;
    }

    public void Handle_SlideCanceled(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Input_SlideHeld = false;
    }
}
