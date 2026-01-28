using Unity.Netcode;
using UnityEngine;

public class AutoNetworkManager : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager component is missing!");
            return;
        }

#if UNITY_EDITOR
        // In editor, do nothing special — behave normally
        Debug.Log("Editor detected: NetworkManager will behave as normal.");
#else
        // Non-editor builds
        AutoStartNetwork();
#endif
    }

    private void AutoStartNetwork()
    {
#if UNITY_SERVER
        StartServer();
#else
        StartClient();
#endif
    }

    private void StartServer()
    {
        if (!networkManager.StartServer())
        {
            Debug.LogError("Failed to start server!");
        }
        else
        {
            Debug.Log("Server started automatically.");
        }
    }

    private void StartClient()
    {
        if (!networkManager.StartClient())
        {
            Debug.LogError("Failed to start client!");
        }
        else
        {
            Debug.Log("Client started automatically.");
        }
    }
}
