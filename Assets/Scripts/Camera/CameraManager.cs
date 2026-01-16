using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField] private CinemachineCamera m_TopDownView, m_ThirdPersonView;

    private void Awake() =>
        instance = this;

    public void SetCameraTarget(Transform orientation)
    {
        m_TopDownView.gameObject.SetActive(false);
        m_ThirdPersonView.gameObject.SetActive(true);

        m_ThirdPersonView.GetComponent<CinemachineTargetGroup>().AddMember(orientation, 1.0f, 1.0f);
    }

    public void RemoveCameraTarget(Transform orientation)
    {
        m_TopDownView.gameObject.SetActive(true);
        m_ThirdPersonView.gameObject.SetActive(false);

        m_ThirdPersonView.GetComponent<CinemachineTargetGroup>().RemoveMember(orientation);
    }

}