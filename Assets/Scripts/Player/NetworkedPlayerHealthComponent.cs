using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public interface IPlayerHealthable
{
    bool IsDead { get; }

    void InitialiseMatch();

    void RequestDamage(float amount, ulong attackerId);

    void SetEnabled(bool enabled);
}


public class NetworkedPlayerHealthComponent
    : NetworkBehaviour, IPlayerHealthable
{
    [SerializeField] private float m_DefaultHealth = 100f;
    [SerializeField] private Image m_HealthBar;

    private NetworkVariable<float> m_Health =
        new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public bool IsDead => m_Health.Value <= 0f;


    /* ================= SERVER ================= */

    public void InitialiseMatch()
    {
        if (!IsServer) return;

        m_Health.Value = m_DefaultHealth;
        SetEnabled(true);
    }


    [Rpc(SendTo.Server)]
    public void RequestDamageRpc(float amount, ulong attackerId)
    {
        if (!IsServer) return;

        if (IsDead)
            return;

        // Basic validation (anti-cheat)
        if (amount <= 0 || amount > 100)
            return;

        ApplyDamage(amount, attackerId);
    }


    private void ApplyDamage(float amount, ulong attackerId)
    {
        m_Health.Value = Mathf.Max(0, m_Health.Value - amount);

        if (m_Health.Value <= 0)
        {
            OnKilled(attackerId);
        }
    }


    private void OnKilled(ulong killerId)
    {
        Debug.Log($"Player {OwnerClientId} killed by {killerId}");

        SetEnabled(false);
    }


    /* ================= CLIENT ================= */

    public void RequestDamage(float amount, ulong attackerId)
    {
        if (IsServer)
        {
            ApplyDamage(amount, attackerId);
        }
        else
        {
            RequestDamageRpc(amount, attackerId);
        }
    }


    /* ================= UI ================= */

    public override void OnNetworkSpawn()
    {
        m_Health.OnValueChanged += OnHealthChanged;
    }


    public override void OnNetworkDespawn()
    {
        m_Health.OnValueChanged -= OnHealthChanged;
    }


    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (m_HealthBar != null)
            m_HealthBar.fillAmount = newValue / m_DefaultHealth;
    }


    public void SetEnabled(bool enabled)
    {
        enabled = enabled;
    }
}
