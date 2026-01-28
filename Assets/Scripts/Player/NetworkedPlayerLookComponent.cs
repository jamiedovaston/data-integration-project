using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public interface IPlayerLookable
{
    void Initialise(Transform orientation, CinemachineCamera camera);
    void Handle_LookAction(InputAction.CallbackContext context);
    void Handle_LeanRPerformed(InputAction.CallbackContext context);
    void Handle_LeanLPerformed(InputAction.CallbackContext context);
}

public class NetworkedPlayerLookComponent : NetworkBehaviour, IPlayerLookable
{
    private Transform m_Orientation;
    private CinemachineCamera m_Camera;

    [Header("Look Settings")]
    [SerializeField] private float lookSpeed = 120f;
    [SerializeField] private float verticalClamp = 70f;
    [SerializeField] private float smoothTime = 0.05f;

    [Header("Lean Settings")]
    [SerializeField] private float m_LeanSpeed = 2.0f;

    [Header("Animation Settings")]
    [SerializeField] private Transform m_Spine;

    private float targetYaw;
    private float targetPitch;

    private float yaw;
    private float pitch;

    private float yawVel;
    private float pitchVel;

    private Vector2 rawLookInput;

    private float currentSideView = 0.5f;
    private bool inputIsLeaningRight = true;

    void IPlayerLookable.Initialise(Transform orientation, CinemachineCamera camera)
    {
        m_Orientation = orientation;
        m_Camera = camera;
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        HandleLook();
        HandleLean();
    }

    private void HandleLook()
    {
        targetYaw += rawLookInput.x * lookSpeed * Time.deltaTime;
        targetPitch -= rawLookInput.y * lookSpeed * Time.deltaTime;

        targetPitch = Mathf.Clamp(targetPitch, -verticalClamp, verticalClamp);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, smoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVel, smoothTime);

        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        m_Orientation.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (m_Spine != null)
        {
            m_Spine.rotation = Quaternion.Euler(pitch, yaw, 0.0f);
        }
    }

    private void HandleLean()
    {
        if (m_Camera == null) return;

        var follow = m_Camera.GetComponent<CinemachineThirdPersonFollow>();
        if (follow == null) return;

        float target = inputIsLeaningRight ? 1f : 0f;

        currentSideView = Mathf.MoveTowards(
            currentSideView,
            target,
            m_LeanSpeed * Time.deltaTime
        );

        follow.CameraSide = currentSideView;
    }

    void IPlayerLookable.Handle_LookAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.canceled)
        {
            rawLookInput = Vector2.zero;
        }
        else
        {
            rawLookInput = context.ReadValue<Vector2>();
        }
    }

    void IPlayerLookable.Handle_LeanRPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed)
            inputIsLeaningRight = true;
    }

    void IPlayerLookable.Handle_LeanLPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed)
            inputIsLeaningRight = false;
    }
}
