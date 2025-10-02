using Photon.Pun;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField] private string playerBlueName = "Player Blue"; // имя в Resources
    [SerializeField] private string playerRedName = "Player Red";  // имя в Resources
    [SerializeField] private Transform baseBlue;
    [SerializeField] private Transform baseRed;

    private void Start()
    {
        if (!PhotonNetwork.InRoom) return;
        var name = PhotonNetwork.IsMasterClient ? playerBlueName : playerRedName;
        PhotonNetwork.Instantiate(name, (PhotonNetwork.IsMasterClient ? baseBlue : baseRed).position, Quaternion.identity);
    }
}