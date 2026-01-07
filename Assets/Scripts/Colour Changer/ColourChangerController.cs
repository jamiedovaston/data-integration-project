using Unity.Netcode;
using UnityEngine;

public class ColourChangerController : NetworkBehaviour
{
    [SerializeField] private MeshRenderer m_ColourChangerMesh;

    private NetworkVariable<Color> m_NetworkColour = new NetworkVariable<Color>();

    public override void OnNetworkSpawn()
    {

        m_NetworkColour.OnValueChanged += UpdateColourRpc;

        m_ColourChangerMesh.material.color = m_NetworkColour.Value;

        if (IsServer) TriggerColourChangerRpc();
    }

    public override void OnNetworkDespawn()
    {
        m_NetworkColour.OnValueChanged -= UpdateColourRpc;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<NetworkObject>().IsLocalPlayer)
            TriggerColourChangerRpc();
    }

    [Rpc(SendTo.Server)]
    private void TriggerColourChangerRpc(RpcParams rpc = default)
    {
        m_NetworkColour.Value = Random.ColorHSV();
    }

    private void UpdateColourRpc(Color previousVal, Color newVal)
    {
        m_ColourChangerMesh.material.color = newVal;
    }
}
