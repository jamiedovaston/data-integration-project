using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerSessionManager : NetworkBehaviour
{
    public static PlayerSessionManager instance;

    Match m_Match;

    [SerializeField] private GameObject m_PlayerObject;
    [SerializeField] private TMP_Text m_CurrentWinnerText;
     
    public List<Transform> m_SpawnAreas = new List<Transform>();

    private Dictionary<ulong, IPlayerable> Matchmaker = new Dictionary<ulong, IPlayerable>();
    public Dictionary<ulong, PlayerServices.UserData_ResultBody> RelationalClientToUserData = new Dictionary<ulong, PlayerServices.UserData_ResultBody>();


    [HideInInspector] public bool playInSession = false;

    private void Awake()
    {
        instance = this;

        m_Match = GetComponent<Match>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;

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
        NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;

        RelationalClientToUserData.Clear();
        Matchmaker.Clear();

        StopAllCoroutines();

        if (IsHost)
        { 
            NetworkManager.ConnectionApprovalCallback -= OnConnectionApprovalCallback;
        }
    }

    private void OnClientConnectedCallback(ulong obj)
    {
        if (!IsServer) return;
        if (NetworkManager.LocalClientId != obj)
            SpawnPlayer(obj);

        if (NetworkManager.ConnectedClientsIds.Count >= 2 && !playInSession)
        {
            playInSession = true;
            StartCoroutine(C_Matchmaking());
        }
    }

    private IEnumerator C_Matchmaking()
    {
        while (playInSession)
        {
            yield return new WaitForSeconds(5.0f);
            if (playInSession)
                yield return StartCoroutine(C_Match());

            foreach (ulong player in NetworkManager.ConnectedClientsIds)
            {
                Matchmaker[player].TeleportRpc(m_SpawnAreas[UnityEngine.Random.Range(0, m_SpawnAreas.Count)].position, Quaternion.identity);
            }
        }
    }
    public IPlayerable GetPlayerByClientId(ulong clientId)
    {
        if (Matchmaker.TryGetValue(clientId, out IPlayerable player))
            return player;
        return null;
    }

    private IEnumerator C_Match()
    {
        int p1 = UnityEngine.Random.Range(0, NetworkManager.ConnectedClientsIds.Count);
        int p2 = UnityEngine.Random.Range(0, NetworkManager.ConnectedClientsIds.Count - 1);
        if (p2 == p1) p2++;
        yield return StartCoroutine(m_Match.C_Match(Matchmaker[NetworkManager.ConnectedClientsIds[p1]], Matchmaker[NetworkManager.ConnectedClientsIds[p2]]));
    }

    private void OnConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Pending = true;
        response.CreatePlayerObject = false;
        response.PlayerPrefabHash = null;

        StartCoroutine(PlayerServices.C_AuthorizeNewPlayer(
        data =>
        {
            var clientID = request.ClientNetworkId;

            RelationalClientToUserData.Add(clientID, data);

            response.Approved = true;
            response.Pending = false;
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
        obj.GetComponent<IPlayerable>().Initialise(RelationalClientToUserData[clientID].username, m_SpawnAreas[UnityEngine.Random.Range(0, m_SpawnAreas.Count)].position);

        Matchmaker.Add(clientID, obj.GetComponent<IPlayerable>());

        Debug.Log($"Authorized new player! Username: {RelationalClientToUserData[clientID].username}, ID: {RelationalClientToUserData[clientID].id}");
    }


    private IEnumerator C_CurrentWinnerTextShowcase(string winner)
    {
        m_CurrentWinnerText.text = $"Winner:  { winner }";
        m_CurrentWinnerText.enabled = true;
        yield return new WaitForSeconds(5.0f);
        m_CurrentWinnerText.enabled = false;
    }

    private void OnClientDisconnectedCallback(ulong clientID)
    {
        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
            NetworkManager.ConnectedClients[clientID].PlayerObject.Despawn();

        if (Matchmaker.ContainsKey(clientID))
            Matchmaker.Remove(clientID);
        if (RelationalClientToUserData.ContainsKey(clientID))
            RelationalClientToUserData.Remove(clientID);

        if (NetworkManager.ConnectedClientsIds.Count < 2 && playInSession)
        {
            playInSession = false;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void DisplayWinnerRpc(string Winner)
    {
        StartCoroutine(C_CurrentWinnerTextShowcase(Winner));
    }
}
