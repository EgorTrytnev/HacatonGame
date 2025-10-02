using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SpawnDetector : MonoBehaviourPun
{
    private bool spawnAllowed = false;
    private SpawnUnits spawnUnits;
    private readonly List<int> myUnitViewIds = new List<int>();

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Spawner"))
        {
            if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            spawnAllowed = true;
            spawnUnits = col.GetComponent<SpawnUnits>();
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Spawner"))
        {
            if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            spawnAllowed = false;
            spawnUnits = null;
        }
    }

    public bool getSpawnAllowed() => spawnAllowed;

    [PunRPC]
    void RPC_SpawnMob(int teamId, string prefabName, Vector3 pos, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var unit = PhotonNetwork.Instantiate(prefabName, pos, Quaternion.identity);
        unit.GetComponent<UnitsDefinition>()?.SetTeam((CollorTeam)teamId);

        var pv = unit.GetComponent<PhotonView>();
        Player requester = PhotonNetwork.CurrentRoom?.GetPlayer(info.Sender.ActorNumber);
        if (pv != null && requester != null) pv.TransferOwnership(requester);

        photonView.RPC(nameof(RPC_RegisterUnit), RpcTarget.All, pv.ViewID, info.Sender.ActorNumber);
    }

    [PunRPC]
    void RPC_RegisterUnit(int unitViewId, int ownerActorNumber)
    {
        if (photonView.Owner != null && photonView.Owner.ActorNumber == ownerActorNumber)
        {
            if (!myUnitViewIds.Contains(unitViewId))
                myUnitViewIds.Add(unitViewId);
        }
    }

    // Команды высокого уровня — обращаются к владельцам юнитов
    public void CmdFollowMe(string unitName, int ownerViewId)
    {
        foreach (int id in myUnitViewIds)
        {
            var pv = PhotonView.Find(id);
            if (pv == null) continue;
            var def = pv.GetComponent<UnitsDefinition>();
            if (def != null && def.GetUnitName().ToLower() == unitName.ToLower())
                pv.RPC("RPC_SetMainTarget", pv.Owner, ownerViewId);
        }
    }

    public void CmdStopFollow(string unitName)
    {
        foreach (int id in myUnitViewIds)
        {
            var pv = PhotonView.Find(id);
            if (pv == null) continue;
            var def = pv.GetComponent<UnitsDefinition>();
            if (def != null && def.GetUnitName().ToLower() == unitName.ToLower())
                pv.RPC("RPC_DeleteMainTarget", pv.Owner);
        }
    }

    public void CmdAttackEnemy(string unitName)
    {
        foreach (int id in myUnitViewIds)
        {
            var pv = PhotonView.Find(id);
            if (pv == null) continue;
            var def = pv.GetComponent<UnitsDefinition>();
            if (def != null && def.GetUnitName().ToLower() == unitName.ToLower())
                pv.RPC("RPC_SetTargetEnemyAuto", pv.Owner);
        }
    }
}
