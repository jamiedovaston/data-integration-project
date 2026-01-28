using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField] private List<GameObject> m_WeaponVFX;
    [SerializeField] private GameObject m_Bullet;

    [Header("Shooter UI")]
    [SerializeField] private Canvas m_PlayerCanvas;
    [SerializeField] private Image m_Crosshair;

    [Header("Aim Settings")]
    [SerializeField] private float m_NeutralFOV = 60.0f;
    [SerializeField] private float m_AimFOV = 30.0f;
    [SerializeField] private float m_AimDelay = 3.0f; 
    
    [Header("Accuracy Settings")]
    [SerializeField] private float m_HipFireSpreadAngle = 2.0f;
    [SerializeField] private float m_AimSpreadAngle = .5f;

    private WeaponModel m_WeaponModel;

    private float lastTimeShot = -1.0f;

    private float currentAimFloat, currentAimFOV;

    private bool Input_Shooting = false;
    private bool Input_Aiming = false;

    void IPlayerShootable.Initialise(CinemachineCamera m_Camera)
    {
        this.m_Camera = m_Camera;

        m_WeaponModel = Instantiate(m_WeaponData.m_Model, m_WeaponHand);
    }

    public override void OnNetworkSpawn()
    {
        m_PlayerCanvas.enabled = NetworkObject.IsLocalPlayer;
    }

    private void Update()
    {
        if (!NetworkObject.IsLocalPlayer)
            return;

        currentAimFloat = Mathf.Lerp(currentAimFloat, Input_Aiming ? 1 : 0, m_AimDelay * Time.deltaTime);

        float fovDiff = m_NeutralFOV - m_AimFOV;
        currentAimFOV = m_AimFOV + fovDiff * (1.0f - currentAimFloat);
        m_Camera.Lens.FieldOfView = currentAimFOV;

        m_Crosshair.transform.localScale  = Vector3.one + Vector3.one * (1.0f - currentAimFloat);

        if (lastTimeShot + m_WeaponData.m_ShootDelay > Time.fixedTime)
            return;

        if (!Input_Shooting)
            return;

        lastTimeShot = Time.fixedTime;

        Vector3 shootOrigin = m_WeaponModel.m_Muzzle.position;
        Vector3 shootTarget;

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(m_Camera.transform.position, m_Camera.transform.forward, out hit, Mathf.Infinity);

        if (hitSomething)
        {
            shootTarget = hit.point;
        }
        else
        {
            shootTarget = m_Camera.transform.position + m_Camera.transform.forward * m_WeaponData.m_Range;
        }

        Vector3 baseDirection = (shootTarget - shootOrigin).normalized;

        float spreadAngle = Input_Aiming ? m_AimSpreadAngle : m_HipFireSpreadAngle;
        Quaternion spreadRotation = Quaternion.Euler(
            UnityEngine.Random.Range(-spreadAngle, spreadAngle),
            UnityEngine.Random.Range(-spreadAngle, spreadAngle),
            0f
        );

        Vector3 finalDirection = spreadRotation * baseDirection;

        RaycastHit hit2;
        bool hitObj = m_WeaponData.Fire(shootOrigin, finalDirection, out hit2);

        if (hit2.collider != null)
        {
            NetworkObject opponent = hit2.collider.GetComponent<NetworkObject>();
            if (opponent != null)
            {
                if (!opponent.IsOwner)
                {
                    IPlayerHealthable health = hit2.collider.GetComponent<IPlayerHealthable>();
                    health.RequestDamage(m_WeaponData.m_Damage, OwnerClientId, transform.position);
                }
            }
        }

        Vector3 vfxTarget = hitObj ? hit2.point : shootOrigin + finalDirection * m_WeaponData.m_Range;
        PlayShootVFX(shootOrigin, vfxTarget);

        ShootRpc(shootOrigin, vfxTarget);
    }


    [Rpc(SendTo.Server)]
    public void ShootRpc(Vector3 pos, Vector3 pos2, RpcParams rpc = default)
    {
        PlayShootVFXRpc(pos, pos2);
    }

    public void PlayShootVFX(Vector3 pos, Vector3 pos2)
    {
        GameObject bulletObj = Instantiate(m_Bullet, pos, Quaternion.identity);

        LineRenderer renderer = bulletObj.GetComponent<LineRenderer>();
        if (renderer == null)
        {
            Debug.LogError("Bullet prefab is missing a LineRenderer!");
            Destroy(bulletObj);
            return;
        }

        renderer.SetPosition(0, pos);
        renderer.SetPosition(1, pos2);

        Destroy(bulletObj, 0.05f);
    }

    [Rpc(SendTo.NotOwner)]
    public void PlayShootVFXRpc(Vector3 pos, Vector3 dir) => PlayShootVFX(pos, dir);

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