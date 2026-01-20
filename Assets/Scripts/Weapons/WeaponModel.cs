using UnityEngine;

public class WeaponModel : MonoBehaviour
{
    [field: SerializeField] public Transform m_Muzzle { get; private set; }

    public enum WeaponAnimType
    {
        Pistol,
        AR
    }
    [field: SerializeField] public WeaponAnimType m_AnimationType { get; private set; }
}