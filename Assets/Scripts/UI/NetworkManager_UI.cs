using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager_UI : NetworkBehaviour
{
    // HOME
    public Canvas m_DisconnectedPanel;

    // LOBBY
    public Canvas m_ConnectedPanel;

    public override void OnNetworkSpawn()
    {
        m_ConnectedPanel.enabled = true;
        m_DisconnectedPanel.enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        m_ConnectedPanel.enabled = false;
        m_DisconnectedPanel.enabled = true;
    }
}
