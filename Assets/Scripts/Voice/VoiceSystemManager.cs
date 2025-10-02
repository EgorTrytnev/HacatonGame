using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoiceSystemManager : MonoBehaviour
{
    public static VoiceSystemManager Instance;

    [Header("Доступные имена для юнитов")]
    public List<string> availableNames = new()
    {
        "жужа", "киса", "цыпа", "зая", "яйцекус"
    };

    [Header("Команды (что сказать → что выполнить)")]
    public List<CommandMapping> commandList = new()
    {
        new CommandMapping { spokenWord = "база",    actionName = "GoToBase" },
        new CommandMapping { spokenWord = "мид",     actionName = "GoToMid" },
        new CommandMapping { spokenWord = "логово",  actionName = "GoToLair" }
    };

    private VoiceCommandProcessor _processor;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        var commandDict = commandList.ToDictionary(c => c.spokenWord, c => c.actionName);
        _processor = new VoiceCommandProcessor(
            allNames: availableNames,
            initialUnitIds: new List<string>(),
            commands: commandDict
        );
    }

    void Start()
    {
        VoiceController.Instance.OnCommandRecognized += OnRawCommand;
    }

    public void RegisterUnit(string unitId)
    {
        _processor.AddUnits(new List<string> { unitId });
    }

    // Публичный метод для получения имени юнита по ID
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

    void OnRawCommand(string rawText)
    {
        var queue = new Queue<string>();
        queue.Enqueue(rawText);
        _processor.ProcessQueue(queue);
    }

    void OnDestroy()
    {
        if (VoiceController.Instance != null)
            VoiceController.Instance.OnCommandRecognized -= OnRawCommand;
    }
    
    private HashSet<string> _awaitingUnits = new HashSet<string>();

    public void SetAwaitingCommand(string unitId, bool awaiting)
    {
        if (awaiting)
            _awaitingUnits.Add(unitId);
        else
            _awaitingUnits.Remove(unitId);

        Debug.Log($"🕒 Юнит {unitId} теперь {(awaiting ? "ожидает команду" : "не ожидает")}");
    }

    public List<string> GetAwaitingUnits() => new List<string>(_awaitingUnits);
}