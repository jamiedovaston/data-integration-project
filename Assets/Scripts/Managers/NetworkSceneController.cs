using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkSceneController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.SceneManager.LoadScene(
            "lobby",
            LoadSceneMode.Additive
        );
    }
}
