using UnityEngine;
using Photon.Pun;

public class InstantiatePlayer : MonoBehaviourPun
{
    [SerializeField] private GameObject cameraPrefab; // локальная камера, без PhotonView

    private PhotonView _view;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (!_view.IsMine) return; // камера только у локального владельца

        // Создаем экземпляр камеры локально
        var camInstance = Instantiate(cameraPrefab, Vector3.zero + new Vector3(0, 0, -10), Quaternion.identity);

        var controller = camInstance.GetComponent<CameraController>();
        if (controller != null)
        {
            controller.SetPlayer(transform);
        }

        // Гарантируем, что камера и слушатель звука активны только локально
        var cam = camInstance.GetComponentInChildren<Camera>(true);
        if (cam) cam.enabled = true;

        var listener = camInstance.GetComponentInChildren<AudioListener>(true);
        if (listener) listener.enabled = true;
    }
}
