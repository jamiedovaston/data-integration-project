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
    private LineRenderer m_Laser;

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

    public override void OnNetworkSpawn()
    {
        if (!NetworkObject.IsLocalPlayer)
            return;

        m_Laser = m_WeaponModel.gameObject.AddComponent<LineRenderer>();
        m_Laser.positionCount = 2;
        m_Laser.material = new Material(Shader.Find("Unlit/Color"));
        m_Laser.material.color = Color.red;
        m_Laser.startWidth = 0.02f;
        m_Laser.endWidth = 0.02f;
        m_Laser.useWorldSpace = true;
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

            UpdateLaser();
        }
    }

    private void UpdateLaser()
    {
        if (!IsLocalPlayer || m_Laser == null)
            return;
        if (m_WeaponModel == null || m_Camera == null)
            return;

        Vector3 cameraOrigin = m_Camera.transform.position;
        Vector3 cameraDir = m_Camera.transform.forward;

        // Step 1: Camera ray determines aim point
        Vector3 aimPoint = cameraOrigin + cameraDir * m_MaxLaserDistance;

        if (Physics.Raycast(cameraOrigin, cameraDir, out RaycastHit camHit, m_MaxLaserDistance))
        {
            aimPoint = camHit.point;
        }

        // Step 2: Muzzle ray goes toward aim point
        Vector3 muzzleOrigin = m_WeaponModel.m_Muzzle.position;
        Vector3 muzzleDir = (aimPoint - muzzleOrigin).normalized;

        Vector3 laserEnd = muzzleOrigin + muzzleDir * m_MaxLaserDistance;

        if (Physics.Raycast(muzzleOrigin, muzzleDir, out RaycastHit muzzleHit, m_MaxLaserDistance))
        {
            laserEnd = muzzleHit.point;
        }

        // Apply to line renderer
        if (m_Laser == null) return;
        m_Laser.SetPosition(0, muzzleOrigin);
        m_Laser.SetPosition(1, laserEnd);
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