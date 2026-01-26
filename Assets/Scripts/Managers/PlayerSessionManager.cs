using Best.HTTP;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSessionManager : NetworkBehaviour
{
    public static PlayerSessionManager instance;

    [SerializeField] private GameObject m_PlayerObject;
     
    [SerializeField] private List<Transform> m_SpawnAreas = new List<Transform>();

    private Dictionary<ulong, bool> ClientInMatch = new Dictionary<ulong, bool>();
    public Dictionary<ulong, PlayerServices.UserData_ResultBody> RelationalClientToUserData = new Dictionary<ulong, PlayerServices.UserData_ResultBody>();

    private void Awake()
    {
        instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        NetworkManager.OnClientDisconnectCallback += OnClientConnectedCallback;

        if (IsHost)
        {
            NetworkManager.ConnectionApprovalCallback += OnConnectionApprovalCallback;
            StartCoroutine(AuthoriseHost());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
        NetworkManager.OnClientDisconnectCallback -= OnClientConnectedCallback;

        if (IsHost)
        {
            NetworkManager.ConnectionApprovalCallback -= OnConnectionApprovalCallback;
        }
    }

    private void OnClientConnectedCallback(ulong obj)
    {
        throw new NotImplementedException();
    }

    private void OnConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Pending = true;

        StartCoroutine(PlayerServices.C_AuthorizeNewPlayer(
        data =>
        {
            var clientID = request.ClientNetworkId;

            ClientInMatch.Add(clientID, false);
            RelationalClientToUserData.Add(clientID, data);

            response.CreatePlayerObject = false;
            response.PlayerPrefabHash = null;
            response.Position = m_SpawnAreas[UnityEngine.Random.Range(0, m_SpawnAreas.Count)].position;

            response.Approved = true;
            response.Pending = false;

            SpawnPlayer(clientID);
        },
        () =>
        {
            response.Reason = "Disconnected from server!";
            response.Approved = false;
            response.Pending = false;
        }));
    }

    private IEnumerator AuthoriseHost()
    {
        yield return StartCoroutine(PlayerServices.C_AuthorizeNewPlayer(
        data =>
        {
            var clientID = NetworkManager.LocalClientId;

            ClientInMatch.Add(clientID, false);
            RelationalClientToUserData.Add(clientID, data);

            SpawnPlayer(clientID);
            Debug.Log("Host authorized successfully");
        },
        () =>
        {
            Debug.LogError("Host authorization failed!");
            NetworkManager.Shutdown();
        }));
    }

    private void SpawnPlayer(ulong clientID)
    {
        GameObject obj = Instantiate(m_PlayerObject, m_SpawnAreas[UnityEngine.Random.Range(0, m_SpawnAreas.Count)].position, Quaternion.identity);
        obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID);

        Debug.Log($"Authorized new player! Username: { RelationalClientToUserData[clientID].username } ID: { RelationalClientToUserData[clientID].id }");
    }

    private void OnClientDisconnectedCallback(ulong clientID)
    {
        if(NetworkManager.ConnectedClients.ContainsKey(clientID))
            NetworkManager.ConnectedClients[clientID].PlayerObject.Despawn();


        if (ClientInMatch.ContainsKey(clientID))
            ClientInMatch.Remove(clientID);
        if (RelationalClientToUserData.ContainsKey(clientID))
            RelationalClientToUserData.Remove(clientID);
    }
}