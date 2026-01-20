using System.Globalization;
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

public class NetworkedPlayerShootComponent : NetworkBehaviour, IPlayerShootable
{
    private CinemachineCamera m_Camera;

    [Header("Shooter Settings")]
    [SerializeField] private WeaponData m_WeaponData;
    [SerializeField] private Transform m_WeaponHand;
    [SerializeField] private LayerMask m_PlayerLayer;

    [Header("Laser Settings")]
    [SerializeField] private float m_MaxLaserDistance = 5.0f;


    [Header("Aim Settings")]
    [SerializeField] private float m_NeutralFOV = 60.0f, m_AimFOV = 30.0f;
    [SerializeField] private float m_AimDelay = 3.0f;

    private WeaponModel m_WeaponModel;

    private float lastTimeShot = -1.0f;

    private float currentAimFOV;

    private bool Input_Shooting = false;
    private bool Input_Aiming = false;

    void IPlayerShootable.Initialise(CinemachineCamera m_Camera)
    {
        this.m_Camera = m_Camera;

        m_WeaponModel = Instantiate(m_WeaponData.m_Model, m_WeaponHand);
    }

    private void Update()
    {
        if (NetworkObject.IsLocalPlayer)
        {
            if (lastTimeShot + m_WeaponData.m_ShootDelay <= Time.fixedTime && Input_Shooting)
            {
                lastTimeShot = Time.fixedTime;

                RaycastHit hit;

                if (Physics.Raycast(m_Camera.transform.position, m_Camera.transform.forward, out hit, Mathf.Infinity))
                {
                    RaycastHit hit2;
                    m_WeaponData.Fire(m_WeaponModel.m_Muzzle.position, hit.point - m_WeaponModel.m_Muzzle.position, out hit2);
                }
            }

            currentAimFOV = Mathf.Lerp(currentAimFOV, (Input_Aiming ? m_AimFOV : m_NeutralFOV), m_AimDelay * Time.deltaTime);
            m_Camera.Lens.FieldOfView = currentAimFOV;
        }
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