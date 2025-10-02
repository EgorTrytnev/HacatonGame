using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Обновленный VoiceSystemManager с исправленной интеграцией VoiceCommandProcessor
/// </summary>
public class VoiceSystemManager : MonoBehaviourPun
{
    [Header("Доступные имена для юнитов")]
    public List<string> availableNames = new()
    {
        "жужа", "киса", "цыпа", "зая", "яйцекус"
    };

    [Header("Команды (что сказать → что выполнить)")]
    public List<CommandMapping> commandList = new()
    {
        new CommandMapping { spokenWord = "база", actionName = "GoToBase" },
        new CommandMapping { spokenWord = "мид", actionName = "GoToMid" },
        new CommandMapping { spokenWord = "логово", actionName = "GoToLair" },
        new CommandMapping { spokenWord = "следуй", actionName = "FollowMe" },
        new CommandMapping { spokenWord = "стой", actionName = "StopFollow" },
        new CommandMapping { spokenWord = "атакуй", actionName = "AttackEnemy" },
        new CommandMapping { spokenWord = "патруль", actionName = "Patrol" }
    };

    private VoiceCommandProcessor _processor;
    private VoiceController _localVoiceController;
    private SpawnDetector _spawnDetector;
    private HashSet<string> _awaitingUnits = new HashSet<string>();
    private Dictionary<string, int> _myUnitViewIds = new Dictionary<string, int>();

    void Awake()
    {
        // Создаем только для локального игрока
        if (!photonView.IsMine && PhotonNetwork.IsConnected) 
        {
            enabled = false;
            return;
        }

        var commandDict = commandList.ToDictionary(c => c.spokenWord, c => c.actionName);
        _processor = new VoiceCommandProcessor(
            allNames: availableNames,
            initialUnitIds: new List<string>(),
            commands: commandDict
        );

        _spawnDetector = GetComponent<SpawnDetector>();
    }

    void Start()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return;

        // Создаем локальный VoiceController только для этого игрока
        GameObject voiceControllerObj = new GameObject($"VoiceController_{photonView.Owner.NickName}");
        _localVoiceController = voiceControllerObj.AddComponent<VoiceController>();
        
        // Подписываемся только на локальный контроллер
        _localVoiceController.OnCommandRecognized += OnRawCommand;

        Debug.Log($"🎙️ Локальная голосовая система активирована для игрока {photonView.Owner.NickName}");
    }

    /// <summary>
    /// Регистрация нового юнита в системе голосового управления
    /// </summary>
    public void RegisterUnit(string unitId, int viewId)
    {
        if (!photonView.IsMine) return;

        _processor.AddUnits(new List<string> { unitId });
        _myUnitViewIds[unitId] = viewId;
        
        Debug.Log($"✅ Юнит {unitId} зарегистрирован с ViewID {viewId}");
    }

    /// <summary>
    /// Удаление юнита из системы
    /// </summary>
    public void UnregisterUnit(string unitId)
    {
        if (!photonView.IsMine) return;

        _processor.RemoveUnits(new List<string> { unitId });
        _myUnitViewIds.Remove(unitId);
        _awaitingUnits.Remove(unitId);
        
        Debug.Log($"🗑️ Юнит {unitId} удален из системы");
    }

    /// <summary>
    /// Получение имени юнита по его ID
    /// </summary>
    public string GetUnitName(string unitId)
    {
        var map = _processor.GetCurrentNameToIdMap();
        foreach (var kvp in map)
        {
            if (kvp.Value == unitId)
                return kvp.Key;
        }
        return null;
    }

    /// <summary>
    /// Установка состояния ожидания команды для юнита
    /// </summary>
    public void SetAwaitingCommand(string unitId, bool awaiting)
    {
        if (!photonView.IsMine) return;

        if (awaiting)
            _awaitingUnits.Add(unitId);
        else
            _awaitingUnits.Remove(unitId);
            
        Debug.Log($"🕒 Юнит {unitId} теперь {(awaiting ? "ожидает команду" : "не ожидает")}");
    }

    /// <summary>
    /// Получение списка юнитов, ожидающих команды
    /// </summary>
    public List<string> GetAwaitingUnits() => new List<string>(_awaitingUnits);

    /// <summary>
    /// Обработка распознанной голосовой команды
    /// </summary>
    void OnRawCommand(string rawText)
    {
        if (!photonView.IsMine) return;

        var queue = new Queue<string>();
        queue.Enqueue(rawText);
        
        // Обрабатываем команду локально, передавая this как VoiceSystemManager
        _processor.ProcessQueue(queue, this);
    }

    /// <summary>
    /// Выполнение действий для юнита через SpawnDetector и EnemyController
    /// </summary>
    void ExecuteUnitActions(string unitId, List<string> actions)
    {
        if (!_myUnitViewIds.ContainsKey(unitId)) return;

        int viewId = _myUnitViewIds[unitId];
        var unitPhotonView = PhotonView.Find(viewId);
        
        if (unitPhotonView == null) return;

        foreach (string action in actions)
        {
            switch (action)
            {
                case "GoToBase":
                case "GoToMid":  
                case "GoToLair":
                case "Patrol":
                    // Отправляем команду перемещения через VoiceCommandBroadcaster
                    VoiceCommandBroadcaster.Broadcast(unitId, new string[] { action });
                    break;
                    
                case "FollowMe":
                    // Используем SpawnDetector для команды следования
                    _spawnDetector?.CmdFollowMe("EnemyUnit", photonView.ViewID);
                    break;
                    
                case "StopFollow":
                    _spawnDetector?.CmdStopFollow("EnemyUnit");
                    break;
                    
                case "AttackEnemy":
                    _spawnDetector?.CmdAttackEnemy("EnemyUnit");
                    break;
                    
                default:
                    Debug.LogWarning($"⚠️ Неизвестное действие: {action}");
                    break;
            }
        }
    }

    /// <summary>
    /// Получить статистику голосовой системы
    /// </summary>
    public string GetVoiceSystemStats()
    {
        if (_processor == null) return "Процессор не инициализирован";
        
        return $"Голосовая система игрока {photonView.Owner.NickName}: " +
               $"{_processor.GetStats()}, Ожидают: {_awaitingUnits.Count}";
    }

    void OnDestroy()
    {
        if (_localVoiceController != null)
        {
            _localVoiceController.OnCommandRecognized -= OnRawCommand;
        }
    }
}