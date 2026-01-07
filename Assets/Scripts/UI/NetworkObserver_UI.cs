using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkObserver_UI : NetworkBehaviour
{
    [SerializeField]
    private NetworkVariable<int> m_PlayerCount = new NetworkVariable<int>(
        value: 0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public TMP_Text m_PlayerCountDisplay;

    public override void OnNetworkSpawn()
    {
        m_PlayerCount.OnValueChanged += SetPlayerCountText;

        if (IsServer)
        {
            m_PlayerCount.Value = 0;

            NetworkManager.Singleton.OnClientConnectedCallback += IncrementPlayerCount;

            NetworkManager.Singleton.OnClientDisconnectCallback += DeincrementPlayerCount;
        }
    }

    public override void OnNetworkDespawn()
    {
        m_PlayerCount.OnValueChanged -= SetPlayerCountText;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= IncrementPlayerCount;
            NetworkManager.Singleton.OnClientDisconnectCallback -= DeincrementPlayerCount;
        }
    }

    public void IncrementPlayerCount(ulong id) => m_PlayerCount.Value++;
    public void DeincrementPlayerCount(ulong id) => m_PlayerCount.Value--;
    public void SetPlayerCountText(int previousVal, int newVal) => m_PlayerCountDisplay.text = $"Players: {newVal}";
}