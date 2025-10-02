using Photon.Pun;
using UnityEngine;

public class UnitInit : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        var data = info.photonView.InstantiationData;
        if (data != null && data.Length > 0)
        {
            int teamId = (int)data[0];
            GetComponent<UnitsDefinition>()?.SetTeam((CollorTeam)teamId);
        }
    }
}
