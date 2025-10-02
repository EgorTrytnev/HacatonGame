using System;
using System.Collections.Generic;
using System.Linq;
using F23.StringSimilarity;
using UnityEngine;

/// <summary>
/// Обработчик голосовых команд - теперь без зависимости от VoiceSystemManager.Instance
/// Работает напрямую с переданным менеджером
/// </summary>
public class VoiceCommandProcessor
{
    private readonly List<string> _allNames;
    private readonly Queue<string> _availableNames;
    private readonly Dictionary<string, string> _nameToId;
    private readonly Dictionary<string, string> _idToName;
    private readonly Dictionary<string, string> _commands;
    private readonly JaroWinkler _similarity = new();
    private readonly double _nameThreshold;
    private readonly double _commandThreshold;

    public VoiceCommandProcessor(
        List<string> allNames,
        List<string> initialUnitIds,
        Dictionary<string, string> commands,
        double nameThreshold = 0.85,
        double commandThreshold = 0.75)
    {
        if (allNames == null || !allNames.Any())
            throw new ArgumentException("Список имён не может быть пустым", nameof(allNames));
        if (initialUnitIds == null)
            throw new ArgumentNullException(nameof(initialUnitIds));
        if (commands == null)
            throw new ArgumentNullException(nameof(commands));

        _allNames = allNames.Select(n => n.ToLowerInvariant()).ToList();
        _availableNames = new Queue<string>(_allNames.OrderBy(_ => Guid.NewGuid())); // случайный порядок
        _nameToId = new Dictionary<string, string>();
        _idToName = new Dictionary<string, string>();
        _commands = commands;
        _nameThreshold = Math.Clamp(nameThreshold, 0.0, 1.0);
        _commandThreshold = Math.Clamp(commandThreshold, 0.0, 1.0);

        AddUnits(initialUnitIds);
    }

    public void AddUnits(List<string> newIds)
    {
        if (newIds == null) return;

        foreach (string id in newIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (_idToName.ContainsKey(id)) continue;

            if (_availableNames.Count == 0)
            {
                Debug.LogWarning($"⚠️ Нет свободных имён для юнита {id}");
                continue;
            }

            string assignedName = _availableNames.Dequeue();
            _idToName[id] = assignedName;
            _nameToId[assignedName] = id;
            Debug.Log($"✅ Юнит {id} получил имя: '{assignedName}'");
        }
    }

    public void RemoveUnits(List<string> idsToRemove)
    {
        if (idsToRemove == null) return;

        foreach (string id in idsToRemove)
        {
            if (_idToName.TryGetValue(id, out string name) && name != null)
            {
                _idToName.Remove(id);
                _nameToId.Remove(name);
                _availableNames.Enqueue(name);
                Debug.Log($"🗑 Юнит {id} удалён, имя '{name}' освобождено");
            }
        }
    }

    public Dictionary<string, string> GetCurrentNameToIdMap()
    {
        return new Dictionary<string, string>(_nameToId);
    }

    private string FindBestMatch(string input, Dictionary<string, string> dictionary, double threshold)
    {
        string bestKey = null;
        double bestScore = 0.0;

        foreach (string key in dictionary.Keys)
        {
            double score = _similarity.Similarity(input, key);
            if (score > bestScore && score >= threshold)
            {
                bestScore = score;
                bestKey = key;
            }
        }

        return bestKey;
    }

    /// <summary>
    /// Обработка очереди команд с передачей VoiceSystemManager
    /// </summary>
    public void ProcessQueue(Queue<string> queue, VoiceSystemManager voiceManager)
    {
        if (queue == null) throw new ArgumentNullException(nameof(queue));
        if (voiceManager == null) throw new ArgumentNullException(nameof(voiceManager));

        while (queue.Count > 0)
        {
            string line = queue.Dequeue();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] words = line.Split(new char[] { ' ', '\t', ',', '.' },
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0) continue;

            var matchedNames = new List<string>();
            var matchedActions = new List<string>();
            var unmatchedWords = new List<string>();

            foreach (string word in words)
            {
                string matchedName = FindBestMatch(word, _nameToId, _nameThreshold);
                if (matchedName != null)
                {
                    matchedNames.Add(matchedName);
                    continue;
                }

                string matchedCmd = FindBestMatch(word, _commands, _commandThreshold);
                if (matchedCmd != null)
                {
                    matchedActions.Add(_commands[matchedCmd]);
                    continue;
                }

                unmatchedWords.Add(word);
            }

            // Обработка результатов
            ProcessParsedCommand(matchedNames, matchedActions, voiceManager);
        }
    }

    /// <summary>
    /// Обработка распарсенной команды
    /// </summary>
    private void ProcessParsedCommand(List<string> matchedNames, List<string> matchedActions, VoiceSystemManager voiceManager)
    {
        // Только имена - активируем ожидание
        if (matchedNames.Count > 0 && matchedActions.Count == 0)
        {
            foreach (string name in matchedNames)
            {
                string id = _nameToId[name];
                voiceManager.SetAwaitingCommand(id, true);
            }
            Debug.Log($"📢 Активировано ожидание для: {string.Join(", ", matchedNames)}");
            return;
        }

        // Только действия - применяем к ожидающим
        if (matchedActions.Count > 0 && matchedNames.Count == 0)
        {
            var awaiting = voiceManager.GetAwaitingUnits();
            if (awaiting.Count > 0)
            {
                foreach (string id in awaiting)
                {
                    VoiceCommandBroadcaster.Broadcast(id, matchedActions.ToArray());
                }

                foreach (string id in awaiting)
                {
                    voiceManager.SetAwaitingCommand(id, false);
                }

                Debug.Log($"📤 Команда применена к ожидающим: {string.Join(", ", awaiting)}");
            }
            else
            {
                Debug.Log("⚠️ Команда без имени, но никто не ожидает");
            }
            return;
        }

        // Имена и действия - прямая команда
        if (matchedNames.Count > 0 && matchedActions.Count > 0)
        {
            foreach (string name in matchedNames)
            {
                string id = _nameToId[name];
                VoiceCommandBroadcaster.Broadcast(id, matchedActions.ToArray());
                voiceManager.SetAwaitingCommand(id, false);
            }
            Debug.Log($"✅ Прямая команда: {string.Join(", ", matchedNames)} → {string.Join(", ", matchedActions)}");
        }
    }

    /// <summary>
    /// Получить все доступные команды
    /// </summary>
    public Dictionary<string, string> GetCommands()
    {
        return new Dictionary<string, string>(_commands);
    }

    /// <summary>
    /// Получить статистику процессора
    /// </summary>
    public string GetStats()
    {
        return $"Имен: {_allNames.Count}, Доступно: {_availableNames.Count}, " +
               $"Назначено: {_nameToId.Count}, Команд: {_commands.Count}";
    }
}