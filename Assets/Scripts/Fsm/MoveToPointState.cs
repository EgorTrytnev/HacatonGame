using UnityEngine;

/// <summary>
/// Состояние ожидания голосовой команды
/// Юнит активно слушает и готов выполнить команду
/// </summary>
public class AwaitingCommandState : BaseState
{
    private float awaitingTime = 0f;
    private float maxAwaitingTime = 10f; // Максимальное время ожидания команды
    private float blinkTimer = 0f;
    private float blinkInterval = 1f;
    private bool isBlinking = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public AwaitingCommandState(UnitFSM fsm) : base(fsm) { }

    public override void OnEnter()
    {
        Debug.Log($"👂 {unit.name}: Жду команду...");
        awaitingTime = 0f;
        blinkTimer = 0f;
        
        // Получаем SpriteRenderer для визуальной индикации
        spriteRenderer = unit.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Воспроизводим звук готовности
        if (fsm != null)
        {
            fsm.PlayRandomResponse();
        }
    }

    public override void OnUpdate()
    {
        awaitingTime += Time.deltaTime;
        blinkTimer += Time.deltaTime;

        // Визуальная индикация ожидания (мигание)
        if (spriteRenderer != null && blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            isBlinking = !isBlinking;
            
            if (isBlinking)
            {
                // Подсвечиваем юнит (делаем ярче)
                spriteRenderer.color = Color.Lerp(originalColor, Color.white, 0.3f);
            }
            else
            {
                // Возвращаем исходный цвет
                spriteRenderer.color = originalColor;
            }
        }

        // Таймаут ожидания
        if (awaitingTime >= maxAwaitingTime)
        {
            Debug.Log($"⏰ {unit.name}: Время ожидания команды истекло, возвращаюсь в режим ожидания");
            
            // Убираем юнит из списка ожидающих
            var voiceManager = FindVoiceSystemManager();
            if (voiceManager != null)
            {
                voiceManager.SetAwaitingCommand(fsm.unitId, false);
            }
            
            fsm.TransitionTo(new IdleState(fsm));
        }
    }

    public override void OnExit()
    {
        // Восстанавливаем исходный цвет
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        Debug.Log($"✋ {unit.name}: Прекращаю ожидание команды");
    }

    /// <summary>
    /// Поиск VoiceSystemManager для управления состоянием ожидания
    /// </summary>
    private VoiceSystemManager FindVoiceSystemManager()
    {
        var players = Object.FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            var photonView = unit.GetComponent<Photon.Pun.PhotonView>();
            var playerPhotonView = player.GetComponent<Photon.Pun.PhotonView>();
            
            if (photonView != null && playerPhotonView != null && 
                photonView.Owner == playerPhotonView.Owner)
            {
                return player.GetComponent<VoiceSystemManager>();
            }
        }
        
        return null;
    }

    public override bool CanBeInterrupted()
    {
        return true; // Ожидание команды может быть прервано
    }

    public override int GetPriority()
    {
        return 3; // Средний приоритет
    }

    /// <summary>
    /// Получить оставшееся время ожидания
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, maxAwaitingTime - awaitingTime);
    }

    /// <summary>
    /// Установить максимальное время ожидания
    /// </summary>
    public void SetMaxAwaitingTime(float time)
    {
        maxAwaitingTime = Mathf.Max(1f, time);
    }

    /// <summary>
    /// Получить прогресс ожидания (0-1)
    /// </summary>
    public float GetAwaitingProgress()
    {
        return maxAwaitingTime > 0 ? Mathf.Clamp01(awaitingTime / maxAwaitingTime) : 1f;
    }

    /// <summary>
    /// Проверить, истекает ли время ожидания
    /// </summary>
    public bool IsTimeRunningOut()
    {
        return GetRemainingTime() <= 2f; // Последние 2 секунды
    }
}