using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public interface IPlayerHealthable
{
    bool IsDead { get; }

    void InitialiseMatch();

    void RequestDamage(float amount, ulong attackerId, Vector3 attackerPosition);

    void SetEnabled(bool enabled);
}

public class NetworkedPlayerHealthComponent : NetworkBehaviour, IPlayerHealthable
{
    [SerializeField] private float m_DefaultHealth = 100f;
    [SerializeField] private Image m_HealthBar;

    private bool m_CanTakeDamage = false; // NEW: control if damage is allowed

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
        SetDamageEnabled(true); // allow damage now
    }

    [Rpc(SendTo.Server)]
    public void RequestDamageRpc(float amount, ulong attackerId, Vector3 attackerPosition)
    {
        if (!IsServer) return;
        ApplyDamage(amount, attackerId, attackerPosition);
    }

    private void ApplyDamage(float amount, ulong attackerId, Vector3 attackerPosition)
    {
        // NEW: skip if damage not allowed
        if (!m_CanTakeDamage) return;

        m_Health.Value = Mathf.Max(0, m_Health.Value - amount);

        if (m_Health.Value <= 0)
        {
            OnKilled(attackerId, attackerPosition);
        }
    }

    private void OnKilled(ulong killerId, Vector3 killerPosition)
    {
        Vector3 deadPos = transform.position;

        // Dead player ping
        StartCoroutine(DataServices.C_PlayerDiedDataPing(
            PlayerSessionManager.instance.RelationalClientToUserData[OwnerClientId].id,
            deadPos.x,
            deadPos.z,
            () => Debug.Log("Dead player data ping success!"),
            () => Debug.Log("Dead player data ping fail!")
        ));

        // Killer ping
        StartCoroutine(DataServices.C_PlayerKilledDataPing(
            PlayerSessionManager.instance.RelationalClientToUserData[killerId].id,
            killerPosition.x,
            killerPosition.z,
            () => Debug.Log("Killer data ping success!"),
            () => Debug.Log("Killer data ping fail!")
        ));

        SetDamageEnabled(false); // disable damage after death
    }

    /* ================= CLIENT ================= */
    public void RequestDamage(float amount, ulong attackerId, Vector3 attackerPosition)
    {
        if (IsServer)
        {
            ApplyDamage(amount, attackerId, attackerPosition);
        }
        else
        {
            RequestDamageRpc(amount, attackerId, attackerPosition);
        }
    }

    /* ================= DAMAGE CONTROL ================= */
    public void SetDamageEnabled(bool canTakeDamage)
    {
        m_CanTakeDamage = canTakeDamage;
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

    // IPlayerHealthable implementation
    public void SetEnabled(bool enabled)
    {
        SetDamageEnabled(enabled);
    }
}
