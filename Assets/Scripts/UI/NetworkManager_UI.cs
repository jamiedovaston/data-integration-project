using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager_UI : NetworkBehaviour
{
    // HOME
    public Canvas m_DisconnectedPanel;
    public Button m_HostButton, m_ClientButton, m_ServerButton;

    // LOBBY
    public Canvas m_ConnectedPanel;
    public Button m_DisconnectButton;

    private void Start()
    {
        m_HostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });

        m_ServerButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });

        m_ClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });

        m_DisconnectButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
        });
    }

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
