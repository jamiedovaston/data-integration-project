using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "JD/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Visuals")]
    [field: SerializeField] public WeaponModel m_Model { get; private set; }

    [Header("Stats")]
    [field: SerializeField] public float m_ShootDelay { get; private set; } = 0.3f;
    [field: SerializeField] public float m_Range { get; private set; } = 15.0f;

    public bool Fire(Vector3 position, Vector3 direction, out RaycastHit hit)
    {
        Debug.DrawRay(position, direction.normalized * m_Range, Color.red, 10.0f);

        if (!Physics.Raycast(position, direction, out hit, m_Range))
            return false;

        if (!hit.collider.TryGetComponent(out NetworkObject netObj))
        {
            netObj = hit.collider.GetComponentInParent<NetworkObject>();
        }

        if (netObj == null)
            return false;

        if (netObj.IsOwner)
            return false;

        Debug.Log("Hit Player!");
        return true;
    }
}
