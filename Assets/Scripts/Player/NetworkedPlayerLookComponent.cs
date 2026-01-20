using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public interface IPlayerLookable
{
    void Initialise(Transform orientation, CinemachineCamera m_Camera);
    void Handle_LookAction(InputAction.CallbackContext context);
    void Handle_LeanRPerformed(InputAction.CallbackContext context);
    void Handle_LeanLPerformed(InputAction.CallbackContext context);
}

public class NetworkedPlayerLookComponent : NetworkBehaviour, IPlayerLookable
{
    Transform m_Orientation;
    CinemachineCamera m_Camera;

    [Header("Look Settings")]
    [SerializeField] private float lookSpeed = 120f;
    [SerializeField] private float verticalClamp = 70f;
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField] private float m_LeanSpeed = 10.0f;

    [Header("Animation Settings")]
    [SerializeField] private Transform m_Spine;

    private float targetYaw;
    private float targetPitch;
    private float yaw;
    private float pitch;
    private float yawVel;
    private float pitchVel;

    private Vector2 rawLookInput;
    private Vector2 smoothedLookInput;
    private Vector2 smoothInputVel;

    private float currentSideView;

    private bool Input_IsLeaningRight = true;

    void IPlayerLookable.Initialise(Transform m_Orientation, CinemachineCamera m_Camera)
    {
        this.m_Orientation = m_Orientation;
        this.m_Camera = m_Camera;
    }

    private void Update()
    {
        if (!IsOwner) return;
        Look();
    }

    private void Look()
    {
        smoothedLookInput = Vector2.SmoothDamp(
            smoothedLookInput,
            rawLookInput,
            ref smoothInputVel,
            smoothTime
        );

        targetYaw += smoothedLookInput.x * lookSpeed * Time.deltaTime;
        targetPitch -= smoothedLookInput.y * lookSpeed * Time.deltaTime;

        targetPitch = Mathf.Clamp(targetPitch, -verticalClamp, verticalClamp);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, smoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVel, smoothTime);
        
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        m_Orientation.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        m_Spine.rotation = Quaternion.Euler(pitch, yaw, 0.0f);

        currentSideView = Mathf.Lerp(currentSideView, (Input_IsLeaningRight ? 1 : 0), m_LeanSpeed * Time.deltaTime);
        m_Camera.GetComponent<CinemachineThirdPersonFollow>().CameraSide = currentSideView;
    }

    void IPlayerLookable.Handle_LookAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        rawLookInput = context.ReadValue<Vector2>();
    }

    void IPlayerLookable.Handle_LeanRPerformed(InputAction.CallbackContext context) => Input_IsLeaningRight = true;

    void IPlayerLookable.Handle_LeanLPerformed(InputAction.CallbackContext context) => Input_IsLeaningRight = false;
}
