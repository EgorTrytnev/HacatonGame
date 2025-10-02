using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Улучшенный UnitsDefinition с поддержкой голосовых команд
/// Добавляет расширенную информацию о юните и голосовое взаимодействие
/// </summary>
public class UnitsDefinition : MonoBehaviour
{
    [Header("Основная информация")]
    [SerializeField] private string nameUnit = "Юнит";
    [SerializeField] private CollorTeam collorTeam = CollorTeam.Red;

    [Header("Голосовые настройки")]
    [SerializeField] private string voiceName = ""; // Имя для голосового распознавания
    [SerializeField] private bool canReceiveVoiceCommands = true;
    [SerializeField] private AudioClip[] statusReportClips;

    [Header("Дополнительная информация")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    // Компоненты
    private AudioSource audioSource;
    private EnemyController enemyController;

    // События
    public System.Action<string> OnVoiceNameChanged;
    public System.Action<CollorTeam> OnTeamChanged;
    public System.Action<float> OnHealthChanged;

    // Состояние
    private Dictionary<string, object> customProperties = new Dictionary<string, object>();

    void Awake()
    {
        // Инициализация компонентов
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D звук
        }

        enemyController = GetComponent<EnemyController>();

        // Если голосовое имя не задано, используем обычное имя
        if (string.IsNullOrEmpty(voiceName))
        {
            voiceName = nameUnit.ToLower();
        }

        // Инициализация здоровья
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Регистрация в голосовой системе, если возможно
        RegisterInVoiceSystem();
    }

    /// <summary>
    /// Регистрация юнита в голосовой системе
    /// </summary>
    void RegisterInVoiceSystem()
    {
        if (!canReceiveVoiceCommands) return;

        // Поиск VoiceSystemManager владельца
        var players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            var photonView = GetComponent<Photon.Pun.PhotonView>();
            var playerPhotonView = player.GetComponent<Photon.Pun.PhotonView>();
            
            if (photonView != null && playerPhotonView != null && 
                photonView.Owner == playerPhotonView.Owner)
            {
                var voiceManager = player.GetComponent<VoiceSystemManager>();
                if (voiceManager != null && enemyController != null)
                {
                    Debug.Log($"🎤 {nameUnit} ({voiceName}) зарегистрирован в голосовой системе");
                    break;
                }
            }
        }
    }

    // ===== ОРИГИНАЛЬНЫЕ МЕТОДЫ =====

    /// <summary>
    /// Получить имя юнита
    /// </summary>
    public string GetUnitName()
    {
        return nameUnit;
    }

    /// <summary>
    /// Установить команду юнита
    /// </summary>
    public void SetTeam(CollorTeam team)
    {
        if (collorTeam != team)
        {
            collorTeam = team;
            OnTeamChanged?.Invoke(team);
            Debug.Log($"🏳️ {nameUnit} сменил команду на {team}");
        }
    }

    /// <summary>
    /// Получить команду юнита
    /// </summary>
    public CollorTeam GetTeam()
    {
        return collorTeam;
    }

    // ===== НОВЫЕ МЕТОДЫ =====

    /// <summary>
    /// Получить голосовое имя юнита
    /// </summary>
    public string GetVoiceName()
    {
        return voiceName;
    }

    /// <summary>
    /// Установить голосовое имя
    /// </summary>
    public void SetVoiceName(string newVoiceName)
    {
        if (voiceName != newVoiceName)
        {
            voiceName = newVoiceName;
            OnVoiceNameChanged?.Invoke(newVoiceName);
            Debug.Log($"🎙️ {nameUnit} получил новое голосовое имя: {newVoiceName}");
        }
    }

    /// <summary>
    /// Получить текущее здоровье
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Получить максимальное здоровье
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Установить здоровье
    /// </summary>
    public void SetHealth(float health)
    {
        float newHealth = Mathf.Clamp(health, 0f, maxHealth);
        if (currentHealth != newHealth)
        {
            currentHealth = newHealth;
            OnHealthChanged?.Invoke(currentHealth);
            
            if (currentHealth <= 0)
            {
                Debug.Log($"💀 {nameUnit} уничтожен");
                PlayStatusReport("Юнит уничтожен!");
            }
            else if (currentHealth < maxHealth * 0.3f)
            {
                Debug.Log($"🩹 {nameUnit} критически ранен ({currentHealth:F0}/{maxHealth:F0})");
                PlayStatusReport("Критические повреждения!");
            }
        }
    }

    /// <summary>
    /// Восстановить здоровье
    /// </summary>
    public void Heal(float amount)
    {
        SetHealth(currentHealth + amount);
        if (amount > 0)
        {
            Debug.Log($"💚 {nameUnit} лечится на {amount} HP");
            PlayStatusReport("Получаю лечение!");
        }
    }

    /// <summary>
    /// Нанести урон
    /// </summary>
    public void TakeDamage(float damage)
    {
        SetHealth(currentHealth - damage);
        if (damage > 0)
        {
            Debug.Log($"💥 {nameUnit} получил {damage} урона");
            PlayStatusReport("Получен урон!");
        }
    }

    /// <summary>
    /// Получить процент здоровья
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (currentHealth / maxHealth) * 100f : 0f;
    }

    /// <summary>
    /// Проверить, жив ли юнит
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    /// <summary>
    /// Проверить, может ли юнит получать голосовые команды
    /// </summary>
    public bool CanReceiveVoiceCommands()
    {
        return canReceiveVoiceCommands && IsAlive();
    }

    /// <summary>
    /// Включить/отключить голосовые команды
    /// </summary>
    public void SetVoiceCommandsEnabled(bool enabled)
    {
        canReceiveVoiceCommands = enabled;
        Debug.Log($"🎙️ Голосовые команды для {nameUnit}: {(enabled ? "включены" : "отключены")}");
    }

    /// <summary>
    /// Воспроизвести голосовой отчет о статусе
    /// </summary>
    void PlayStatusReport(string message)
    {
        if (statusReportClips != null && statusReportClips.Length > 0 && audioSource != null)
        {
            AudioClip clip = statusReportClips[Random.Range(0, statusReportClips.Length)];
            audioSource.PlayOneShot(clip);
        }

        Debug.Log($"📢 {nameUnit}: {message}");
    }

    /// <summary>
    /// Установить пользовательское свойство
    /// </summary>
    public void SetCustomProperty(string key, object value)
    {
        customProperties[key] = value;
    }

    /// <summary>
    /// Получить пользовательское свойство
    /// </summary>
    public T GetCustomProperty<T>(string key, T defaultValue = default(T))
    {
        if (customProperties.ContainsKey(key) && customProperties[key] is T)
        {
            return (T)customProperties[key];
        }
        return defaultValue;
    }

    /// <summary>
    /// Получить полную информацию о юните
    /// </summary>
    public string GetFullInfo()
    {
        return $"{nameUnit} ({voiceName}) - Команда: {collorTeam}, " +
               $"Здоровье: {currentHealth:F0}/{maxHealth:F0} ({GetHealthPercentage():F1}%), " +
               $"Голос: {(canReceiveVoiceCommands ? "Вкл" : "Выкл")}";
    }

    // ===== ГОЛОСОВЫЕ КОМАНДЫ =====

    /// <summary>
    /// Голосовая команда: доложить статус
    /// </summary>
    public void VoiceCommand_ReportStatus()
    {
        string status = $"Здоровье {GetHealthPercentage():F0}%, готов к выполнению приказов";
        PlayStatusReport(status);
    }

    void OnDestroy()
    {
        Debug.Log($"🗑️ {nameUnit} ({voiceName}) уничтожен");
    }
}