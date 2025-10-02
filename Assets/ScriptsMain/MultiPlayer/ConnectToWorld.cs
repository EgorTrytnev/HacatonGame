using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class ConnectToWorld : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField createRoomInput;
    [SerializeField] private TMP_InputField joinRoomInput;

    private void Start()
    {
        //PhotonNetwork.AutomaticallySyncScene = true;

    }

    public void CreateRoom()
    {
        var name = string.IsNullOrWhiteSpace(createRoomInput.text) ? "Room_1" : createRoomInput.text;
        
        PhotonNetwork.CreateRoom(name);
    }

    public void JoinRoom()
    {
        var name = string.IsNullOrWhiteSpace(joinRoomInput.text) ? "Room_1" : joinRoomInput.text;
        PhotonNetwork.JoinRoom(name);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("MainGame");
        Debug.Log("Joined the game");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"CreateRoomFailed {returnCode}: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"JoinRoomFailed {returnCode}: {message}");
    }
}
