using System;
using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Match : NetworkBehaviour
{

    public NetworkVariable<FixedString32Bytes> m_CurrentWinner = new NetworkVariable<FixedString32Bytes>();
    [SerializeField] private Transform[] m_MatchSpawnPositions = new Transform[2];
    [SerializeField] private TMP_Text m_WaitingForMatch;
    private IPlayerable[] player = new IPlayerable[2];

    public IEnumerator C_Match(IPlayerable p1, IPlayerable p2)
    {
        Debug.Log("Match started!");
        player = new IPlayerable[2] { p1, p2 };
        player[0].m_Health.InitialiseMatch();
        player[1].m_Health.InitialiseMatch();

        foreach (IPlayerable p in player)
        {
            WaitingForMatchmakingSignRpc(
                false,
                new RpcSendParams
                {
                    Target = RpcTarget.Single(p.NetObject.OwnerClientId, RpcTargetUse.Temp)
                }
            );
        }

        yield return StartCoroutine(C_StartMatchProcess());
        yield return StartCoroutine(C_Gameplay());
        EndMatch();
    }

    public IEnumerator C_StartMatchProcess()
    {
        Debug.Log("Match Process Started!");
        player[0].LockMovementRpc(true);
        player[1].LockMovementRpc(true);
        player[0].TeleportRpc(m_MatchSpawnPositions[0].position, m_MatchSpawnPositions[0].rotation);
        player[1].TeleportRpc(m_MatchSpawnPositions[1].position, m_MatchSpawnPositions[1].rotation);

        yield return new WaitForSeconds(3.0f);

        if (!PlayerInvalid(player[0]))
            player[0].LockMovementRpc(false);
        if (!PlayerInvalid(player[1]))
            player[1].LockMovementRpc(false);
    }

    public IEnumerator C_Gameplay()
    {
        Debug.Log("Match Gameplay Started!");

        while (true)
        {
            
            if (PlayerInvalid(player[0]))
                break;

            if (PlayerInvalid(player[1]))
                break;

            yield return null;
        }

        Debug.Log("Match Gameplay Ended");
    }

    public void EndMatch()
    {
        if (!IsServer)
            return;

        int winner = player[1].m_Health.IsDead ? 0 : 1;

        PlayerSessionManager.instance.DisplayWinnerRpc(
            PlayerSessionManager.instance.RelationalClientToUserData[player[winner].NetObject.OwnerClientId].username
        );

        WaitingForMatchmakingSignRpc(true, new RpcSendParams { Target = RpcTarget.Everyone });

        for (int i = 0; i < 2; i++)
        {
            if (!PlayerInvalid(player[i]))
            {
                player[i].TeleportRpc(
                    PlayerSessionManager.instance.m_SpawnAreas[UnityEngine.Random.Range(0, PlayerSessionManager.instance.m_SpawnAreas.Count)].position,
                    Quaternion.identity
                );
            }
        }

        player = null;

        Debug.Log("Match ended!");
    }

    public override void OnNetworkSpawn()
    {
        // NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }


    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();
        // NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    // private void OnClientDisconnectCallback(ulong obj)
    // {
    //     if (!IsServer) return;
    // 
    //     if (obj == player[0].NetObject.OwnerClientId || obj == player[1].NetObject.OwnerClientId)
    //     {
    // 
    //     }
    // }

    bool PlayerInvalid(IPlayerable p)
    {
        if (p == null)
            return true;

        if (p.NetObject == null)
            return true;

        if (!p.NetObject.IsSpawned)
            return true;

        if (p.m_Health == null)
            return true;

        if (p.m_Health.IsDead)
            return true;

        return false;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void WaitingForMatchmakingSignRpc(bool active, RpcParams rpc = default)
    {
        if (m_WaitingForMatch == null)
            return;

        m_WaitingForMatch.enabled = active;
    }

}