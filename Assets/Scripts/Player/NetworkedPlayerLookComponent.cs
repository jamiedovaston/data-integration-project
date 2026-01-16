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

    [Header("Look")]
    [SerializeField] float lookSpeed = 120f;
    [SerializeField] float verticalClamp = 70f;
    [SerializeField] float smoothTime = 0.05f;

    private float targetYaw;
    private float targetPitch;
    private float yaw;
    private float pitch;
    private float yawVel;
    private float pitchVel;

    private float rotationX;

    Vector2 Input_Look = Vector2.zero;

    public void Initialise(Transform m_Orientation)
    {
        this.m_Orientation = m_Orientation;
    }

    private void Update() => Look();

    private void Look()
    {
        targetYaw += Input_Look.x * lookSpeed * Time.fixedDeltaTime;
        targetPitch -= Input_Look.y * lookSpeed * Time.fixedDeltaTime;

        targetPitch = Mathf.Clamp(targetPitch, -verticalClamp, verticalClamp);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, smoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVel, smoothTime);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        m_Orientation.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void Handle_LookAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        Input_Look = context.ReadValue<Vector2>();
    }

}
