using System.Collections;
using System;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Улучшенный EnemyController с поддержкой голосовых команд
/// Исправлена ошибка с UnityEngine.Random
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(UnitsDefinition))]
public class EnemyController : MonoBehaviourPun
{
    [Header("Голосовые настройки")]
    public bool respondToVoice = true;
    public AudioClip[] responseClips;
    public float responseDelay = 0.5f;

    // Компоненты
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Animator _animator;
    private Detector _detectorEnemy;
    private HeatPointsController _hp;
    private UnitsDefinition _unitDefinition;
    private AudioSource _audioSource;
    private VoiceSystemManager _voiceSystemManager;

    // Голосовая система
    [HideInInspector] public string unitId;
    private static int _nextId = 1;
    private bool _nameLogged = false;

    // События жизненного цикла
    public event Action OnUnitDestroyed;
    public event Action<string> OnVoiceCommandReceived;

    // Patrol / move
    private Vector2 centerPoint;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float multiplySpeed = 1.5f;

    // Combat
    [SerializeField] private int speedAttack = 5;

    // Targets
    private Transform target;
    private Transform currentAttacker;
    [SerializeField] private float stopRadiusPlayer = 2f;
    [SerializeField] private float stopRadiusEnemy = 0.3f;
    [SerializeField] private float pursuitRadius = 6f;

    // Steering
    [SerializeField] private float slowRadius = 3f;
    [SerializeField] private float seekWeight = 1.0f;
    [SerializeField] private float fleeWeight = 1.6f;
    [SerializeField] private float dangerRadius = 2.0f;

    // States
    private bool isFollowPlayer = false;
    private bool isRandAction = true;
    private bool isAttacking = false;

    // Movement
    private Vector2 desiredVelocity = Vector2.zero;
    private Coroutine attackCoroutine;
    private Coroutine patrolCoroutine;

    // Anti-stick windows
    [SerializeField] private float followLockDuration = 1.5f;
    [SerializeField] private float reaggroBlockDuration = 0.8f;
    private float followLockUntil = -1f;
    private float reaggroBlockUntil = -1f;

    // Дополнительные точки для голосовых команд
    private Transform basePoint;
    private Transform midPoint;
    private Transform lairPoint;

    bool HasAuth => photonView.IsMine;

    void Awake()
    {
        // Генерируем уникальный ID для голосовой системы
        unitId = "Unit_" + _nextId++;
    }

    void Start()
    {
        // Инициализация компонентов
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;
        _animator = GetComponent<Animator>();
        _detectorEnemy = GetComponent<Detector>();
        _hp = GetComponent<HeatPointsController>();
        _unitDefinition = GetComponent<UnitsDefinition>();

        // Настройка аудио
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D звук
        }

        // Инициализация физики
        centerPoint = transform.position;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        // Поиск точек для голосовых команд
        FindNavigationPoints();

        // Настройка голосовой системы
        SetupVoiceSystem();

        // Запуск патрулирования для владельца
        if (HasAuth)
            patrolCoroutine = StartCoroutine(WanderPatrol());
    }

    /// <summary>
    /// Настройка голосовой системы
    /// </summary>
    void SetupVoiceSystem()
    {
        if (!respondToVoice) return;

        // Находим VoiceSystemManager владельца юнита
        var ownerPlayer = FindOwnerPlayer();
        if (ownerPlayer != null)
        {
            _voiceSystemManager = ownerPlayer.GetComponent<VoiceSystemManager>();
            if (_voiceSystemManager != null && HasAuth)
            {
                // Регистрируем юнит в голосовой системе
                _voiceSystemManager.RegisterUnit(unitId, photonView.ViewID);
                Debug.Log($"🎤 Юнит {unitId} ({_unitDefinition.GetUnitName()}) зарегистрирован в голосовой системе");
            }
        }

        // Подписываемся на голосовые команды
        VoiceCommandBroadcaster.OnCommandReceived += OnVoiceCommand;
    }

    /// <summary>
    /// Поиск игрока-владельца юнита
    /// </summary>
    GameObject FindOwnerPlayer()
    {
        var players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            var playerPV = player.GetComponent<PhotonView>();
            if (playerPV != null && playerPV.Owner == photonView.Owner)
            {
                return player.gameObject;
            }
        }

        // Fallback: поиск локального игрока
        foreach (var player in players)
        {
            var playerPV = player.GetComponent<PhotonView>();
            if (playerPV != null && playerPV.IsMine)
            {
                return player.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Поиск навигационных точек для голосовых команд
    /// </summary>
    void FindNavigationPoints()
    {
        basePoint = GameObject.Find("Point_Base")?.transform;
        midPoint = GameObject.Find("Point_Mid")?.transform;
        lairPoint = GameObject.Find("Point_Lair")?.transform;

        if (basePoint == null) Debug.LogWarning("⚠️ Point_Base не найдена");
        if (midPoint == null) Debug.LogWarning("⚠️ Point_Mid не найдена");
        if (lairPoint == null) Debug.LogWarning("⚠️ Point_Lair не найдена");
    }

    void Update()
    {
        if (!HasAuth) return;

        // Логирование имени юнита (один раз)
        if (!_nameLogged && _voiceSystemManager != null)
        {
            string myName = _voiceSystemManager.GetUnitName(unitId);
            if (!string.IsNullOrEmpty(myName))
            {
                Debug.Log($"✅ Я — {myName} (ID: {unitId}, Unity: {_unitDefinition.GetUnitName()})");
                _nameLogged = true;
            }
        }

        // Логика движения (оригинальная)
        if (isRandAction) return;

        if (target == null)
        {
            StopAttack();
            SetMovementActive(true);
            return;
        }

        if (isFollowPlayer) FollowToPlayer_Steering();
        else FollowToEnemy();
    }

    void FixedUpdate()
    {
        if (!HasAuth) return;
        _rb.linearVelocity = desiredVelocity;
    }

    /// <summary>
    /// Обработка голосовых команд
    /// </summary>
    void OnVoiceCommand(string targetId, string[] actions)
    {
        if (targetId != unitId || !respondToVoice || !HasAuth) return;

        Debug.Log($"🎙️ {unitId} получил голосовую команду: {string.Join(", ", actions)}");

        foreach (string action in actions)
        {
            OnVoiceCommandReceived?.Invoke(action);
            ExecuteVoiceAction(action);
        }

        // Воспроизводим звук подтверждения
        if (responseClips != null && responseClips.Length > 0)
        {
            Invoke(nameof(PlayRandomResponse), responseDelay);
        }

        // Сбрасываем ожидание команды
        if (_voiceSystemManager != null)
        {
            _voiceSystemManager.SetAwaitingCommand(unitId, false);
        }
    }

    /// <summary>
    /// Выполнение голосовых команд
    /// </summary>
    void ExecuteVoiceAction(string action)
    {
        switch (action)
        {
            case "GoToBase":
                if (basePoint != null)
                {
                    MoveToPoint(basePoint);
                    Debug.Log($"🎯 {unitId} движется к базе");
                }
                break;

            case "GoToMid":
                if (midPoint != null)
                {
                    MoveToPoint(midPoint);
                    Debug.Log($"🎯 {unitId} движется к середине");
                }
                break;

            case "GoToLair":
                if (lairPoint != null)
                {
                    MoveToPoint(lairPoint);
                    Debug.Log($"🎯 {unitId} движется к логову");
                }
                break;

            case "FollowMe":
                var owner = FindOwnerPlayer();
                if (owner != null)
                {
                    SetMainTarget(owner.transform);
                    Debug.Log($"👥 {unitId} следует за игроком");
                }
                break;

            case "StopFollow":
                DeleteMainTarget();
                Debug.Log($"⏹️ {unitId} прекратил следование");
                break;

            case "AttackEnemy":
                SetTargetEnemy();
                Debug.Log($"⚔️ {unitId} атакует врага");
                break;

            case "Patrol":
                StartPatrolMode();
                Debug.Log($"🚶 {unitId} начинает патрулирование");
                break;
        }
    }

    /// <summary>
    /// Движение к определенной точке (для голосовых команд)
    /// </summary>
    void MoveToPoint(Transform point)
    {
        if (point == null) return;

        StopAttack();
        SetMovementActive(false);
        target = point;
        isFollowPlayer = true; // Используем логику следования
        currentAttacker = null;
        followLockUntil = Time.time + followLockDuration;
    }

    /// <summary>
    /// Начало режима патрулирования
    /// </summary>
    void StartPatrolMode()
    {
        DeleteMainTarget();
        centerPoint = transform.position;
    }

    /// <summary>
    /// Воспроизведение случайного звука подтверждения
    /// </summary>
    public void PlayRandomResponse()
    {
        if (responseClips == null || responseClips.Length == 0) return;

        AudioClip clip = responseClips[UnityEngine.Random.Range(0, responseClips.Length)];
        _audioSource.PlayOneShot(clip);
        
        Debug.Log($"🔊 {unitId} воспроизводит звук подтверждения");
    }

    // ===== ОРИГИНАЛЬНЫЙ ФУНКЦИОНАЛ ENEMYCONTROLLER =====

    private Vector2 GetNewTargetPosition()
    {
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * maxDistance;
        return centerPoint + randomOffset;
    }

    IEnumerator WanderPatrol()
    {
        while (true)
        {
            if (!isRandAction)
            {
                desiredVelocity = Vector2.zero;
                yield return null;
                continue;
            }

            Vector2 targetPos = GetNewTargetPosition();
            float distance = Vector2.Distance(transform.position, targetPos);

            while (distance > 0.1f)
            {
                if (!isRandAction) break;
                Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
                desiredVelocity = direction * moveSpeed;
                distance = Vector2.Distance(transform.position, targetPos);
                yield return null;
            }

            desiredVelocity = Vector2.zero;
            float waitTime = UnityEngine.Random.Range(3f, 6f);
            float elapsed = 0f;

            while (elapsed < waitTime)
            {
                if (!isRandAction) break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            centerPoint = transform.position;
        }
    }

    // ===== PUBLIC API (оригинальные методы) =====

    public void SetMainTarget(Transform newTarget)
    {
        int id = ToViewId(newTarget);
        if (HasAuth) RPC_SetMainTarget(id);
        else photonView.RPC(nameof(RPC_SetMainTarget), photonView.Owner, id);
    }

    public void DeleteMainTarget()
    {
        if (HasAuth) RPC_DeleteMainTarget();
        else photonView.RPC(nameof(RPC_DeleteMainTarget), photonView.Owner);
    }

    public void SetTargetEnemy()
    {
        if (HasAuth) RPC_SetTargetEnemyAuto();
        else photonView.RPC(nameof(RPC_SetTargetEnemyAuto), photonView.Owner);
    }

    public void SetTargetEnemy(Transform targetEnemy)
    {
        int id = ToViewId(targetEnemy);
        if (HasAuth) RPC_SetTargetEnemy(id);
        else photonView.RPC(nameof(RPC_SetTargetEnemy), photonView.Owner, id);
    }

    // ===== RPC МЕТОДЫ (оригинальные) =====

    [PunRPC] 
    void RPC_SetMainTarget(int targetViewId)
    {
        StopAttack();
        SetMovementActive(false);
        target = FromViewId(targetViewId);
        isFollowPlayer = target != null;
        currentAttacker = null;
        if (isFollowPlayer)
            followLockUntil = Time.time + followLockDuration;
    }

    [PunRPC] 
    void RPC_DeleteMainTarget()
    {
        SetMovementActive(true);
        target = null;
        isFollowPlayer = false;
        currentAttacker = null;
        reaggroBlockUntil = Time.time + reaggroBlockDuration;
    }

    [PunRPC] 
    void RPC_SetTargetEnemyAuto()
    {
        SetMovementActive(false);
        target = _detectorEnemy != null ? _detectorEnemy.DetectTargetAuth() : null;
        isFollowPlayer = false;
        if (target == null) SetMovementActive(true);
        else SetSpeedForLeaveFunc();
    }

    [PunRPC] 
    void RPC_SetTargetEnemy(int targetViewId)
    {
        SetMovementActive(false);
        target = FromViewId(targetViewId);
        isFollowPlayer = false;
        if (target == null) SetMovementActive(true);
        else SetSpeedForLeaveFunc();
    }

    [PunRPC]
    void RPC_MoveToPoint(string pointName)
    {
        GameObject point = GameObject.Find(pointName);
        if (point != null)
        {
            MoveToPoint(point.transform);
        }
    }

    // ===== БОЕВАЯ СИСТЕМА (оригинальная) =====

    void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;
    }

    IEnumerator AttackAction()
    {
        isAttacking = true;
        yield return new WaitForSeconds(speedAttack);
        if (isFollowPlayer || target == null) { isAttacking = false; yield break; }

        // Урон
        var hp = target.GetComponentInParent<HeatPointsController>();
        var hpPv = hp ? hp.GetComponent<PhotonView>() : null;
        if (hpPv != null)
            hpPv.RPC("RPC_TakeDamage", hpPv.Owner, 1);

        // Реакция жертвы
        var victimEc = target.GetComponentInParent<EnemyController>();
        var victimPv = victimEc ? victimEc.GetComponent<PhotonView>() : null;
        if (victimPv != null)
            victimPv.RPC(nameof(RPC_ReactByAttack), victimPv.Owner, photonView.ViewID, false);

        isAttacking = false;
    }

    [PunRPC]
    void RPC_ReactByAttack(int attackerViewId, bool forceChase = false)
    {
        var attackerT = FromViewId(attackerViewId);
        if (attackerT == null) return;

        // Follow-замок
        if (!forceChase && isFollowPlayer && Time.time < followLockUntil)
        {
            currentAttacker = attackerT;
            SetSpeedForLeaveFunc();
            return;
        }

        // Блок ре-агра
        if (!forceChase && Time.time < reaggroBlockUntil)
        {
            currentAttacker = attackerT;
            return;
        }

        // Стандартная погоня
        isFollowPlayer = false;
        SetMovementActive(false);
        SetTargetEnemy(attackerT);
    }

    // ===== ДВИЖЕНИЕ (оригинальные методы) =====

    public void SetMovementActive(bool active)
    {
        isRandAction = active;
        desiredVelocity = Vector2.zero;
        if (!HasAuth) return;

        if (active)
        {
            if (patrolCoroutine == null)
                patrolCoroutine = StartCoroutine(WanderPatrol());
            centerPoint = transform.position;
        }
        else
        {
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }
            _rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetSpeedForLeaveFunc() => StartCoroutine(SpeedBoost());

    public IEnumerator SpeedBoost()
    {
        float originalSpeed = moveSpeed;
        moveSpeed = originalSpeed * multiplySpeed;
        yield return new WaitForSeconds(2f);
        moveSpeed = originalSpeed;
    }

    private void FollowToPlayer_Steering()
    {
        if (target == null) { desiredVelocity = Vector2.zero; return; }

        Vector2 pos = _rb.position;
        Vector2 toPlayer = (Vector2)target.position - pos;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer <= stopRadiusPlayer)
        {
            desiredVelocity = Vector2.zero;
            currentAttacker = null;
            return;
        }

        Vector2 dirSeek = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.zero;
        Vector2 dirFlee = Vector2.zero;
        float wFlee = fleeWeight;

        if (currentAttacker != null)
        {
            Vector2 fromAttacker = pos - (Vector2)currentAttacker.position;
            if (fromAttacker.sqrMagnitude > 0.0001f) dirFlee = fromAttacker.normalized;
            float dA = fromAttacker.magnitude;
            if (dA < dangerRadius) wFlee *= 1.5f;
        }

        Vector2 blended = (seekWeight * dirSeek) + (wFlee * dirFlee);
        if (blended.sqrMagnitude > 0.0001f) blended.Normalize();

        float slowFactor = distToPlayer < slowRadius ? (distToPlayer / slowRadius) : 1f;
        desiredVelocity = blended * moveSpeed * slowFactor;
    }

    private void FollowToEnemy()
    {
        if (target == null) { SetMovementActive(true); return; }

        float distToTarget = Vector2.Distance(transform.position, target.position);

        if (distToTarget > pursuitRadius)
        {
            StopAttack();
            target = null;
            SetMovementActive(true);
            return;
        }

        Vector2 toEnemy = (Vector2)target.position - _rb.position;
        float distance = toEnemy.magnitude;

        desiredVelocity = (distance > stopRadiusEnemy)
            ? (distance > 0.0001f ? toEnemy.normalized * moveSpeed : Vector2.zero)
            : Vector2.zero;

        if (_detectorEnemy != null && _detectorEnemy.GetCanHitAuth() && !isAttacking)
            attackCoroutine = StartCoroutine(AttackAction());
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

    static int ToViewId(Transform t)
    {
        if (t == null) return 0;
        var pv = t.GetComponentInParent<PhotonView>();
        return pv != null ? pv.ViewID : 0;
    }

    static Transform FromViewId(int id) => id != 0 ? PhotonView.Find(id)?.transform : null;

    // ===== СОБЫТИЯ ЖИЗНЕННОГО ЦИКЛА =====

    void OnDestroy()
    {
        // Отписываемся от событий
        VoiceCommandBroadcaster.OnCommandReceived -= OnVoiceCommand;

        // Удаляем из голосовой системы
        if (_voiceSystemManager != null && HasAuth)
        {
            _voiceSystemManager.UnregisterUnit(unitId);
        }

        // Уведомляем об уничтожении
        OnUnitDestroyed?.Invoke();

        Debug.Log($"🗑️ Юнит {unitId} уничтожен");
    }

    void OnDisable()
    {
        // Остановка корутин
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
    }
}