using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private List<GameObject> m_WeaponVFX;

    [Header("Aim Settings")]
    [SerializeField] private float m_NeutralFOV = 60.0f;
    [SerializeField] private float m_AimFOV = 30.0f;
    [SerializeField] private float m_AimDelay = 3.0f; 
    
    [Header("Accuracy Settings")]
    [SerializeField] private float m_SpreadAngle = 1.5f;

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
                    Vector3 baseDirection = (hit.point - m_WeaponModel.m_Muzzle.position).normalized;

                    Quaternion spreadRotation = Quaternion.Euler(
                        UnityEngine.Random.Range(-m_SpreadAngle, m_SpreadAngle),
                        UnityEngine.Random.Range(-m_SpreadAngle, m_SpreadAngle),
                        0f
                    );

                    Vector3 direction = spreadRotation * baseDirection;
                    m_WeaponData.Fire(m_WeaponModel.m_Muzzle.position, direction, out hit2);
                    ShootRpc(transform.position.x, transform.position.z);

                    foreach (GameObject vFX in m_WeaponVFX)
                    {
                        Destroy(Instantiate(vFX, m_WeaponModel.m_Muzzle.position, Quaternion.LookRotation(direction)), 1.0f);
                    }
                }
            }

            currentAimFOV = Mathf.Lerp(currentAimFOV, (Input_Aiming ? m_AimFOV : m_NeutralFOV), m_AimDelay * Time.deltaTime);
            m_Camera.Lens.FieldOfView = currentAimFOV;
        }
    }

    [Rpc(SendTo.Server)]
    public void ShootRpc(float x, float y, RpcParams rpc = default) => StartCoroutine(C_Shoot(x, y, rpc));

    IEnumerator C_Shoot(float x, float y, RpcParams rpc)
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