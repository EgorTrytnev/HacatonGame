using UnityEngine;
using System;

/// <summary>
/// Обновленный UnitFSM с интеграцией новых состояний
/// Полная совместимость с системой голосового управления
/// </summary>
public class UnitFSM : MonoBehaviour
{
    [Header("Аудио ответы")]
    public AudioClip[] responseClips;
    
    [Header("Голосовые настройки")]
    public bool respondToVoice = true;
    public float responseDelay = 0.5f;

    [Header("Настройки состояний")]
    public float moveSpeed = 3f;
    public float maxAwaitingTime = 10f;

    private AudioSource _audioSource;
    [HideInInspector] public string unitId;
    private BaseState currentState;
    private static int _nextId = 1;
    private bool _nameLogged = false;
    private VoiceSystemManager _voiceSystemManager;

    // События жизненного цикла юнита
    public event Action OnUnitDestroyed;
    public event Action<string> OnCommandReceived;
    public event Action<BaseState, BaseState> OnStateChanged;

    void Awake()
    {
        unitId = "Unit_" + _nextId++;
        _audioSource = GetComponent<AudioSource>();
        
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D звук
        }
    }

    void Start()
    {
        // Находим VoiceSystemManager владельца юнита
        var player = FindLocalPlayer();
        if (player != null)
        {
            _voiceSystemManager = player.GetComponent<VoiceSystemManager>();
        }

        if (_voiceSystemManager == null)
        {
            Debug.LogError("VoiceSystemManager не найден! Юнит не зарегистрирован.");
            respondToVoice = false;
        }

        // Подписываемся на голосовые команды
        VoiceCommandBroadcaster.OnCommandReceived += OnVoiceCommand;
        
        // Начинаем с состояния ожидания
        TransitionTo(new IdleState(this));
    }

    void Update()
    {
        currentState?.OnUpdate();

        // Логируем имя ОДИН РАЗ, когда оно становится доступно
        if (!_nameLogged && _voiceSystemManager != null)
        {
            string myName = _voiceSystemManager.GetUnitName(unitId);
            if (!string.IsNullOrEmpty(myName))
            {
                Debug.Log($"✅ Я — {myName} (ID: {unitId})");
                _nameLogged = true;
            }
        }

        // Проверка ожидания команды
        if (_voiceSystemManager != null && respondToVoice)
        {
            bool isAwaiting = _voiceSystemManager.GetAwaitingUnits().Contains(unitId);
            if (isAwaiting && !(currentState is AwaitingCommandState))
            {
                // Переходим в состояние ожидания команды, если текущее состояние может быть прервано
                if (currentState == null || currentState.CanBeInterrupted())
                {
                    var awaitingState = new AwaitingCommandState(this);
                    awaitingState.SetMaxAwaitingTime(maxAwaitingTime);
                    TransitionTo(awaitingState);
                }
            }
        }
    }

    /// <summary>
    /// Переход к новому состоянию с уведомлением
    /// </summary>
    public void TransitionTo(BaseState newState)
    {
        BaseState oldState = currentState;
        
        // Проверяем приоритеты состояний
        if (oldState != null && newState != null && 
            oldState.GetPriority() > newState.GetPriority() && 
            !oldState.CanBeInterrupted())
        {
            Debug.Log($"⚠️ {unitId}: Не могу перейти к {newState.GetStateName()}, текущее состояние {oldState.GetStateName()} имеет высший приоритет");
            return;
        }

        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();
        
        // Уведомляем об изменении состояния
        OnStateChanged?.Invoke(oldState, newState);
        
        string oldStateName = oldState?.GetStateName() ?? "None";
        string newStateName = newState?.GetStateName() ?? "None";
        Debug.Log($"🔄 {unitId}: {oldStateName} → {newStateName}");
    }

    /// <summary>
    /// Обработка голосовых команд
    /// </summary>
    void OnVoiceCommand(string targetId, string[] actions)
    {
        if (targetId != unitId || !respondToVoice) return;

        // Сбрасываем ожидание при получении команды
        _voiceSystemManager?.SetAwaitingCommand(unitId, false);

        foreach (string action in actions)
        {
            OnCommandReceived?.Invoke(action);
            ExecuteAction(action);
        }

        // Воспроизводим звук подтверждения с задержкой
        if (responseClips != null && responseClips.Length > 0)
        {
            Invoke(nameof(PlayRandomResponse), responseDelay);
        }
    }

    /// <summary>
    /// Выполнение конкретного действия
    /// </summary>
    void ExecuteAction(string action)
    {
        switch (action)
        {
            case "GoToBase":
                GoToPoint("Point_Base");
                break;
                
            case "GoToMid":
                GoToPoint("Point_Mid");
                break;
                
            case "GoToLair":
                GoToPoint("Point_Lair");
                break;
                
            case "Patrol":
                StartPatrolling();
                break;
                
            case "Guard":
                StartGuarding();
                break;
                
            case "Return":
                ReturnToPlayer();
                break;

            case "Stop":
                TransitionTo(new IdleState(this));
                break;
                
            default:
                Debug.LogWarning($"⚠️ Неизвестное действие для {unitId}: {action}");
                break;
        }
    }

    /// <summary>
    /// Перемещение к указанной точке
    /// </summary>
    void GoToPoint(string pointName)
    {
        GameObject point = GameObject.Find(pointName);
        if (point == null)
        {
            Debug.LogError($"❌ Точка '{pointName}' не найдена!");
            return;
        }

        var moveState = new MoveToPointState(this, point.transform.position, pointName);
        moveState.SetSpeed(moveSpeed);
        TransitionTo(moveState);
    }

    /// <summary>
    /// Начало патрулирования
    /// </summary>
    void StartPatrolling()
    {
        // Находим точки патрулирования
        var patrolPoints = GameObject.FindGameObjectsWithTag("PatrolPoint");
        if (patrolPoints.Length > 0)
        {
            TransitionTo(new PatrolState(this, patrolPoints));
        }
        else
        {
            Debug.LogWarning("⚠️ Точки патрулирования не найдены");
            // Альтернативное поведение - случайное патрулирование
            TransitionTo(new RandomPatrolState(this));
        }
    }

    /// <summary>
    /// Начало охраны позиции
    /// </summary>
    void StartGuarding()
    {
        TransitionTo(new GuardState(this, transform.position));
    }

    /// <summary>
    /// Возврат к игроку
    /// </summary>
    void ReturnToPlayer()
    {
        var player = FindLocalPlayer();
        if (player != null)
        {
            TransitionTo(new FollowState(this, player.transform));
        }
        else
        {
            Debug.LogWarning("⚠️ Локальный игрок не найден");
        }
    }

    /// <summary>
    /// Поиск локального игрока (владельца юнитов)
    /// </summary>
    GameObject FindLocalPlayer()
    {
        var players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            var pv = player.GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return player.gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// Воспроизведение случайного аудио-ответа
    /// </summary>
    public void PlayRandomResponse()
    {
        if (responseClips == null || responseClips.Length == 0)
        {
            Debug.LogWarning($"Нет аудио для ответа у юнита {unitId}");
            return;
        }

        AudioClip clip = responseClips[UnityEngine.Random.Range(0, responseClips.Length)];
        _audioSource.PlayOneShot(clip);
        
        Debug.Log($"🔊 {unitId} воспроизводит звук подтверждения");
    }

    /// <summary>
    /// Получение текущего состояния юнита
    /// </summary>
    public string GetCurrentStateName()
    {
        return currentState?.GetStateName() ?? "None";
    }

    /// <summary>
    /// Проверка, может ли юнит получать команды
    /// </summary>
    public bool CanReceiveCommands()
    {
        return respondToVoice && _voiceSystemManager != null;
    }

    /// <summary>
    /// Принудительная активация/деактивация голосового управления
    /// </summary>
    public void SetVoiceControlEnabled(bool enabled)
    {
        respondToVoice = enabled;
        
        if (!enabled && _voiceSystemManager != null)
        {
            _voiceSystemManager.SetAwaitingCommand(unitId, false);
        }
        
        Debug.Log($"🎙️ Голосовое управление для {unitId}: {(enabled ? "включено" : "отключено")}");
    }

    /// <summary>
    /// Получить текущее состояние (для внешнего доступа)
    /// </summary>
    public BaseState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Принудительно остановить все действия
    /// </summary>
    public void ForceStop()
    {
        TransitionTo(new IdleState(this));
    }

    void OnDisable()
    {
        VoiceCommandBroadcaster.OnCommandReceived -= OnVoiceCommand;
        currentState?.OnExit();
        
        // Уведомляем об уничтожении
        OnUnitDestroyed?.Invoke();
    }
}

// ===== ДОПОЛНИТЕЛЬНЫЕ СОСТОЯНИЯ =====

/// <summary>
/// Состояние патрулирования между точками
/// </summary>
public class PatrolState : BaseState
{
    private GameObject[] patrolPoints;
    private int currentPointIndex = 0;
    private MoveToPointState moveState;

    public PatrolState(UnitFSM fsm, GameObject[] points) : base(fsm)
    {
        patrolPoints = points;
    }

    public override void OnEnter()
    {
        Debug.Log($"🚶 {unit.name} начинает патрулирование между {patrolPoints.Length} точками");
        MoveToNextPoint();
    }

    public override void OnUpdate()
    {
        moveState?.OnUpdate();
        
        // Проверяем, достигли ли мы точки
        if (moveState != null && Vector3.Distance(unit.transform.position, moveState.GetTargetPosition()) < 0.5f)
        {
            MoveToNextPoint();
        }
    }

    private void MoveToNextPoint()
    {
        if (patrolPoints.Length == 0) return;
        
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        Vector3 nextPoint = patrolPoints[currentPointIndex].transform.position;
        moveState = new MoveToPointState(fsm, nextPoint, $"PatrolPoint_{currentPointIndex}");
        moveState.OnEnter();
    }

    public override int GetPriority()
    {
        return 4;
    }
}

/// <summary>
/// Состояние случайного патрулирования
/// </summary>
public class RandomPatrolState : BaseState
{
    private Vector3 basePosition;
    private float patrolRadius = 5f;
    private MoveToPointState moveState;

    public RandomPatrolState(UnitFSM fsm) : base(fsm)
    {
        basePosition = unit.transform.position;
    }

    public override void OnEnter()
    {
        Debug.Log($"🔀 {unit.name} начинает случайное патрулирование");
        MoveToRandomPoint();
    }

    public override void OnUpdate()
    {
        moveState?.OnUpdate();
        
        if (moveState != null && Vector3.Distance(unit.transform.position, moveState.GetTargetPosition()) < 0.5f)
        {
            MoveToRandomPoint();
        }
    }

    private void MoveToRandomPoint()
    {
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = basePosition + new Vector3(randomOffset.x, randomOffset.y, 0);
        moveState = new MoveToPointState(fsm, randomPoint, "RandomPoint");
        moveState.OnEnter();
    }

    public override int GetPriority()
    {
        return 3;
    }
}

/// <summary>
/// Состояние охраны позиции
/// </summary>
public class GuardState : BaseState
{
    private Vector3 guardPosition;
    private float guardRadius = 2f;

    public GuardState(UnitFSM fsm, Vector3 position) : base(fsm)
    {
        guardPosition = position;
    }

    public override void OnEnter()
    {
        Debug.Log($"🛡️ {unit.name} охраняет позицию");
    }

    public override void OnUpdate()
    {
        // Возвращаемся на позицию, если отошли слишком далеко
        if (Vector3.Distance(unit.transform.position, guardPosition) > guardRadius)
        {
            Vector3 direction = (guardPosition - unit.transform.position).normalized;
            unit.transform.position += direction * 2f * Time.deltaTime;
        }
    }

    public override int GetPriority()
    {
        return 6;
    }
}

/// <summary>
/// Состояние следования за целью
/// </summary>
public class FollowState : BaseState
{
    private Transform target;
    private float followDistance = 2f;

    public FollowState(UnitFSM fsm, Transform target) : base(fsm)
    {
        this.target = target;
    }

    public override void OnEnter()
    {
        Debug.Log($"👥 {unit.name} следует за целью");
    }

    public override void OnUpdate()
    {
        if (target == null)
        {
            fsm.TransitionTo(new IdleState(fsm));
            return;
        }

        float distance = Vector3.Distance(unit.transform.position, target.position);
        if (distance > followDistance)
        {
            Vector3 direction = (target.position - unit.transform.position).normalized;
            unit.transform.position += direction * 3f * Time.deltaTime;
        }
    }

    public override int GetPriority()
    {
        return 7;
    }
}