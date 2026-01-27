using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private List<GameObject> m_WeaponVFX;
    [SerializeField] private GameObject m_Bullet;

    [Header("Aim Settings")]
    [SerializeField] private float m_NeutralFOV = 60.0f;
    [SerializeField] private float m_AimFOV = 30.0f;
    [SerializeField] private float m_AimDelay = 3.0f; 
    
    [Header("Accuracy Settings")]
    [SerializeField] private float m_HipFireSpreadAngle = 2.0f;
    [SerializeField] private float m_AimSpreadAngle = .5f;

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
        if (!NetworkObject.IsLocalPlayer)
            return;

        currentAimFOV = Mathf.Lerp(currentAimFOV, Input_Aiming ? m_AimFOV : m_NeutralFOV, m_AimDelay * Time.deltaTime);
        m_Camera.Lens.FieldOfView = currentAimFOV;

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
        bool hitPlayer = m_WeaponData.Fire(shootOrigin, finalDirection, out hit2);

        Vector3 vfxTarget = hitPlayer ? hit2.point : shootOrigin + finalDirection * m_WeaponData.m_Range;
        PlayShootVFX(shootOrigin, vfxTarget);

        ShootRpc(shootOrigin, vfxTarget, transform.position.x, transform.position.z);
    }


    [Rpc(SendTo.Server)]
    public void ShootRpc(Vector3 pos, Vector3 pos2, float x, float y, RpcParams rpc = default)
    {
        PlayShootVFXRpc(pos, pos2);

        StartCoroutine(C_ShootDataPing(x, y, rpc));
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

    IEnumerator C_ShootDataPing(float x, float y, RpcParams rpc)
    {
        yield return DataServices.C_PlayerKilledDataPing(PlayerSessionManager.instance.RelationalClientToUserData[rpc.Receive.SenderClientId].id, x, y, () =>
        {
            Debug.Log("Success!");
        },
        () =>
        {
            Debug.Log("Fail!");
        });

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