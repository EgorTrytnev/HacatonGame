using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

/// <summary>
/// Модифицированный SpawnDetector с поддержкой голосовых команд
/// Добавляет события для интеграции с VoiceSystemManager
/// </summary>
public class SpawnDetector : MonoBehaviourPun
{
    private bool spawnAllowed = false;
    private SpawnUnits spawnUnits;
    private readonly List<int> myUnitViewIds = new List<int>();

    // События для интеграции с голосовой системой
    public event Action<GameObject> OnUnitSpawned;
    public event Action<GameObject> OnUnitDestroyed;

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

    /// <summary>
    /// RPC для спавна юнитов с уведомлением голосовой системы
    /// </summary>
    [PunRPC]
    void RPC_SpawnMob(int teamId, string prefabName, Vector3 pos, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var unit = PhotonNetwork.Instantiate(prefabName, pos, Quaternion.identity);
        unit.GetComponent<UnitsDefinition>()?.SetTeam((CollorTeam)teamId);

        var pv = unit.GetComponent<PhotonView>();
        Player requester = PhotonNetwork.CurrentRoom?.GetPlayer(info.Sender.ActorNumber);

        if (pv != null && requester != null) 
        {
            pv.TransferOwnership(requester);
            
            // Уведомляем о спавне юнита
            photonView.RPC(nameof(RPC_RegisterUnit), RpcTarget.All, pv.ViewID, info.Sender.ActorNumber);
        }
    }

    /// <summary>
    /// Регистрация юнита с расширенным функционалом
    /// </summary>
    [PunRPC]
    void RPC_RegisterUnit(int unitViewId, int ownerActorNumber)
    {
        if (photonView.Owner != null && photonView.Owner.ActorNumber == ownerActorNumber)
        {
            if (!myUnitViewIds.Contains(unitViewId))
            {
                myUnitViewIds.Add(unitViewId);
                
                // Находим объект юнита и настраиваем его
                var unitPV = PhotonView.Find(unitViewId);
                if (unitPV != null)
                {
                    var unitObj = unitPV.gameObject;
                    
                    // Уведомляем о спавне юнита для голосовой системы
                    OnUnitSpawned?.Invoke(unitObj);
                    
                    // Подписываемся на уничтожение юнита
                    var unitFSM = unitObj.GetComponent<UnitFSM>();
                    if (unitFSM != null)
                    {
                        unitFSM.OnUnitDestroyed += () => {
                            OnUnitDestroyed?.Invoke(unitObj);
                            myUnitViewIds.Remove(unitViewId);
                        };
                    }
                }
            }
        }
    }

    /// <summary>
    /// Команда следования с поддержкой голосовых команд
    /// </summary>
    public void CmdFollowMe(string unitName, int ownerViewId)
    {
        ExecuteUnitCommand(unitName, (pv, def) => 
        {
            pv.RPC("RPC_SetMainTarget", pv.Owner, ownerViewId);
            Debug.Log($"🎯 {def.GetUnitName()} теперь следует за игроком");
        });
    }

    /// <summary>
    /// Команда остановки следования
    /// </summary>
    public void CmdStopFollow(string unitName)
    {
        ExecuteUnitCommand(unitName, (pv, def) => 
        {
            pv.RPC("RPC_DeleteMainTarget", pv.Owner);
            Debug.Log($"⏹️ {def.GetUnitName()} остановлен");
        });
    }

    /// <summary>
    /// Команда атаки противника
    /// </summary>
    public void CmdAttackEnemy(string unitName)
    {
        ExecuteUnitCommand(unitName, (pv, def) => 
        {
            pv.RPC("RPC_SetTargetEnemyAuto", pv.Owner);
            Debug.Log($"⚔️ {def.GetUnitName()} атакует врага");
        });
    }

    /// <summary>
    /// Команда перемещения к точке (для голосовых команд)
    /// </summary>
    public void CmdMoveToPoint(string unitName, string pointName)
    {
        ExecuteUnitCommand(unitName, (pv, def) => 
        {
            pv.RPC("RPC_MoveToPoint", pv.Owner, pointName);
            Debug.Log($"🚶 {def.GetUnitName()} движется к {pointName}");
        });
    }

    /// <summary>
    /// Универсальный метод для выполнения команд юнита
    /// </summary>
    private void ExecuteUnitCommand(string unitName, System.Action<PhotonView, UnitsDefinition> action)
    {
        foreach (int id in myUnitViewIds)
        {
            var pv = PhotonView.Find(id);
            if (pv == null) continue;

            var def = pv.GetComponent<UnitsDefinition>();
            if (def != null && def.GetUnitName().ToLower().Contains(unitName.ToLower()))
            {
                action(pv, def);
                return;
            }
        }
        
        Debug.LogWarning($"⚠️ Юнит с именем '{unitName}' не найден");
    }

    /// <summary>
    /// Получение всех юнитов игрока
    /// </summary>
    public List<GameObject> GetMyUnits()
    {
        var units = new List<GameObject>();
        
        foreach (int id in myUnitViewIds)
        {
            var pv = PhotonView.Find(id);
            if (pv != null)
            {
                units.Add(pv.gameObject);
            }
        }
        
        return units;
    }

    /// <summary>
    /// Получение количества юнитов игрока
    /// </summary>
    public int GetUnitCount() => myUnitViewIds.Count;

    /// <summary>
    /// Проверка, принадлежит ли юнит этому игроку
    /// </summary>
    public bool IsMyUnit(int viewId) => myUnitViewIds.Contains(viewId);
}