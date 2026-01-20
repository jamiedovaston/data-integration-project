using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerShootable
{
    void Initialise(CinemachineCamera m_Camera);
    void Handle_ShootPerformed(InputAction.CallbackContext context);
    void Handle_ShootCanceled(InputAction.CallbackContext context);
    void Handle_AimPerformed(InputAction.CallbackContext context);
    void Handle_AimCanceled(InputAction.CallbackContext context);
}

public class NetworkedPlayerShootComponent : MonoBehaviour, IPlayerShootable
{
    private CinemachineCamera m_Camera;

    [Header("Shooter Settings")]
    [SerializeField] private float m_Delay = 0.3f;
    [SerializeField] private LayerMask m_PlayerLayer;

    [Header("Aim Settings")]
    [SerializeField] private float m_NeutralFOV = 60.0f, m_AimFOV = 30.0f;
    [SerializeField] private float m_AimDelay = 3.0f;

    private float lastTimeShot = -1.0f;

    private float currentAimFOV;

    private bool Input_Shooting = false;
    private bool Input_Aiming = false;

    void IPlayerShootable.Initialise(CinemachineCamera m_Camera)
    {
        this.m_Camera = m_Camera;
    }

    private void Update()
    {
        if (lastTimeShot + m_Delay <= Time.time && Input_Shooting)
        {
            lastTimeShot = Time.time;

            RaycastHit hit;

            if (Physics.Raycast(m_Camera.transform.position, m_Camera.transform.forward, out hit, Mathf.Infinity, m_PlayerLayer))
            {
                if (hit.collider.GetComponent<NetworkObject>().IsLocalPlayer) return;
                
                Debug.Log("Hit Player!");
            }
        }

        currentAimFOV = Mathf.Lerp(currentAimFOV, (Input_Aiming ? m_AimFOV : m_NeutralFOV), m_AimDelay * Time.deltaTime);
        m_Camera.Lens.FieldOfView = currentAimFOV;
    }

    void IPlayerShootable.Handle_AimPerformed(InputAction.CallbackContext context)
    {
        Input_Aiming = true;
    }

    void IPlayerShootable.Handle_AimCanceled(InputAction.CallbackContext context)
    {
        Input_Aiming = false;
    }

    void IPlayerShootable.Handle_ShootPerformed(InputAction.CallbackContext context)
    {
        Input_Shooting = true;
    }

    void IPlayerShootable.Handle_ShootCanceled(InputAction.CallbackContext context)
    {
        Input_Shooting = false;
    }
}