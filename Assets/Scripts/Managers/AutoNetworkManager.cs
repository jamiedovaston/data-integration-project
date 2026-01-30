using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class AutoNetworkManager : MonoBehaviour
{
    void Start()
    {
        // Default values
        string ip = "0.0.0.0";
        ushort port = 7777;

        // Read from environment variables if set
        string envIp = Environment.GetEnvironmentVariable("GAME_SERVER_IP");
        string envPort = Environment.GetEnvironmentVariable("GAME_SERVER_PORT");

        if (!string.IsNullOrEmpty(envIp))
            ip = envIp;

        if (!string.IsNullOrEmpty(envPort) && ushort.TryParse(envPort, out ushort parsedPort))
            port = parsedPort;

        Debug.Log($"Starting server on {ip}:{port}");

        // Set transport data
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport component not found on NetworkManager!");
            return;
        }

        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = port;

        // Start server
        NetworkManager.Singleton.StartServer();
    }
}
