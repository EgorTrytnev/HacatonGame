using Photon.Pun;
using UnityEngine;

// Вешается на префаб Player
public class PlayerInit : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [SerializeField] private GameObject cameraPrefab; // локальная камера без PhotonView

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Назначить команду по тому, кто заспавнил объект (мастер = синий, иначе = красный)
        bool isBlue = info.Sender != null && info.Sender.IsMasterClient;
        GetComponent<UnitsDefinition>()?.SetTeam(isBlue ? CollorTeam.Blue : CollorTeam.Red);

        // Создать локальную камеру только у владельца
        if (photonView.IsMine && cameraPrefab != null)
        {
            var camInstance = Instantiate(cameraPrefab, transform.position + new Vector3(0, 0, -10), Quaternion.identity);
            var controller = camInstance.GetComponent<CameraController>();
            if (controller != null) controller.SetPlayer(transform);
        }
    }
}
