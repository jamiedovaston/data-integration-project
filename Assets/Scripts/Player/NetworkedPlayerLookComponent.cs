using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerLookable
{
    void Initialise(Transform orientation);
    void Handle_LookAction(InputAction.CallbackContext context);
}

public class NetworkedPlayerLookComponent : NetworkBehaviour, IPlayerLookable
{
    Transform m_Orientation;

    [Header("Look Settings")]
    [SerializeField] float lookSpeed = 120f;
    [SerializeField] float verticalClamp = 70f;
    [SerializeField] float smoothTime = 0.05f;

    private float targetYaw;
    private float targetPitch;
    private float yaw;
    private float pitch;
    private float yawVel;
    private float pitchVel;

    private Vector2 rawLookInput;
    private Vector2 smoothedLookInput;
    private Vector2 smoothInputVel;

    public void Initialise(Transform m_Orientation)
    {
        this.m_Orientation = m_Orientation;
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
    }

    public void Handle_LookAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        rawLookInput = context.ReadValue<Vector2>();
    }
}
