using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Match : NetworkBehaviour
{
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
            if (p.NetObject.IsLocalPlayer)
            {
                m_WaitingForMatch.enabled = false;
            }
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

        player[0].LockMovementRpc(false);
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
        player = null;
        m_WaitingForMatch.enabled = true;
        Debug.Log("Match ended!");
    }

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
}