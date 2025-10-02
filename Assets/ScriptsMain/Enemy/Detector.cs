using Photon.Pun;
using UnityEngine;

/// <summary>
/// Улучшенный Detector с поддержкой голосовых команд
/// Исправлены ошибки с Gizmos.DrawWireCircle
/// </summary>
public class Detector : MonoBehaviourPun
{
    [Header("Настройки обнаружения")]
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float viewAngle = 360f;
    [SerializeField] public LayerMask targetMask;
    [SerializeField] public LayerMask obstacleMask;

    [Header("Голосовые настройки")]
    [SerializeField] private bool enableVoiceReporting = true;
    [SerializeField] private AudioClip detectionSound;

    private bool canHit = false;
    private Transform lastDetectedTarget;
    private AudioSource audioSource;

    // События для голосовой системы
    public System.Action<Transform> OnTargetDetected;
    public System.Action<Transform> OnTargetLost;
    public System.Action<Transform> OnCanHitChanged;

    void Start()
    {
        // Инициализация аудио компонента
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && detectionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D звук
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Проверяем изменения в обнаружении целей
        var currentTarget = DetectTargetAuth();
        
        if (currentTarget != lastDetectedTarget)
        {
            if (lastDetectedTarget != null && currentTarget == null)
            {
                // Цель потеряна
                OnTargetLost?.Invoke(lastDetectedTarget);
                if (enableVoiceReporting)
                {
                    Debug.Log($"🔍 {gameObject.name} потерял цель: {lastDetectedTarget.name}");
                }
            }
            else if (lastDetectedTarget == null && currentTarget != null)
            {
                // Новая цель обнаружена
                OnTargetDetected?.Invoke(currentTarget);
                PlayDetectionSound();
                if (enableVoiceReporting)
                {
                    Debug.Log($"🎯 {gameObject.name} обнаружил цель: {currentTarget.name}");
                }
            }
            
            lastDetectedTarget = currentTarget;
        }
    }

    /// <summary>
    /// Обнаружение цели с проверкой авторитета (оригинальный метод с улучшениями)
    /// </summary>
    public Transform DetectTargetAuth()
    {
        if (!photonView.IsMine) return null;

        Transform unitTransform = null;
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);

        float closestDistance = float.MaxValue;
        Transform closestTarget = null;

        foreach (Collider2D c in targetsInViewRadius)
        {
            Vector2 dirToTarget = (c.transform.position - transform.position).normalized;
            float dist = Vector2.Distance(transform.position, c.transform.position);

            // Проверка угла обзора (если не 360°)
            if (viewAngle < 360f)
            {
                float angle = Vector2.Angle(transform.up, dirToTarget);
                if (angle > viewAngle * 0.5f) continue;
            }

            // Проверка препятствий
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToTarget, dist, obstacleMask);
            
            var targetUnit = c.GetComponent<UnitsDefinition>();
            var myUnit = GetComponent<UnitsDefinition>();

            if (targetUnit != null && myUnit != null && hit.collider == null && targetUnit.GetTeam() != myUnit.GetTeam())
            {
                // Выбираем ближайшую цель
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = c.transform;
                }
            }
        }

        return closestTarget;
    }

    /// <summary>
    /// Получение всех видимых целей (для голосовых команд)
    /// </summary>
    public Transform[] DetectAllTargets()
    {
        if (!photonView.IsMine) return new Transform[0];

        var targets = new System.Collections.Generic.List<Transform>();
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);

        foreach (Collider2D c in targetsInViewRadius)
        {
            Vector2 dirToTarget = (c.transform.position - transform.position).normalized;
            float dist = Vector2.Distance(transform.position, c.transform.position);

            // Проверка угла обзора
            if (viewAngle < 360f)
            {
                float angle = Vector2.Angle(transform.up, dirToTarget);
                if (angle > viewAngle * 0.5f) continue;
            }

            // Проверка препятствий
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToTarget, dist, obstacleMask);
            
            var targetUnit = c.GetComponent<UnitsDefinition>();
            var myUnit = GetComponent<UnitsDefinition>();

            if (targetUnit != null && myUnit != null && hit.collider == null && targetUnit.GetTeam() != myUnit.GetTeam())
            {
                targets.Add(c.transform);
            }
        }

        return targets.ToArray();
    }

    /// <summary>
    /// Поиск цели определенного типа (для голосовых команд)
    /// </summary>
    public Transform DetectTargetByName(string targetName)
    {
        if (!photonView.IsMine) return null;

        var allTargets = DetectAllTargets();
        
        foreach (var target in allTargets)
        {
            var unitDef = target.GetComponent<UnitsDefinition>();
            if (unitDef != null && unitDef.GetUnitName().ToLower().Contains(targetName.ToLower()))
            {
                return target;
            }
        }

        return null;
    }

    /// <summary>
    /// Проверка возможности атаки (оригинальный метод)
    /// </summary>
    public bool GetCanHitAuth() => photonView.IsMine && canHit;

    /// <summary>
    /// Получение количества обнаруженных целей
    /// </summary>
    public int GetTargetCount()
    {
        return DetectAllTargets().Length;
    }

    /// <summary>
    /// Воспроизведение звука обнаружения
    /// </summary>
    void PlayDetectionSound()
    {
        if (audioSource != null && detectionSound != null)
        {
            audioSource.PlayOneShot(detectionSound);
        }
    }

    /// <summary>
    /// Установка радиуса обзора (для голосовых команд)
    /// </summary>
    public void SetViewRadius(float radius)
    {
        viewRadius = Mathf.Max(0f, radius);
        if (enableVoiceReporting)
        {
            Debug.Log($"🔍 {gameObject.name} изменил радиус обзора на {viewRadius}");
        }
    }

    /// <summary>
    /// Установка угла обзора (для голосовых команд)
    /// </summary>
    public void SetViewAngle(float angle)
    {
        viewAngle = Mathf.Clamp(angle, 0f, 360f);
        if (enableVoiceReporting)
        {
            Debug.Log($"🔍 {gameObject.name} изменил угол обзора на {viewAngle}°");
        }
    }

    // ===== ОРИГИНАЛЬНЫЕ МЕТОДЫ COLLISION DETECTION =====

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!photonView.IsMine) return;

        var targetUnit = collision.GetComponent<UnitsDefinition>();
        var myUnit = GetComponent<UnitsDefinition>();

        if (targetUnit != null && myUnit != null && targetUnit != myUnit)
        {
            bool wasCanHit = canHit;
            canHit = true;

            // Уведомляем об изменении возможности атаки
            if (!wasCanHit)
            {
                OnCanHitChanged?.Invoke(collision.transform);
                if (enableVoiceReporting)
                {
                    Debug.Log($"⚔️ {gameObject.name} может атаковать {collision.name}");
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!photonView.IsMine) return;

        var targetUnit = collision.GetComponent<UnitsDefinition>();
        var myUnit = GetComponent<UnitsDefinition>();

        if (targetUnit != null && myUnit != null && targetUnit != myUnit)
        {
            bool wasCanHit = canHit;
            canHit = false;

            // Уведомляем об изменении возможности атаки
            if (wasCanHit)
            {
                OnCanHitChanged?.Invoke(collision.transform);
                if (enableVoiceReporting)
                {
                    Debug.Log($"🛡️ {gameObject.name} не может атаковать {collision.name}");
                }
            }
        }
    }

    // ===== ГОЛОСОВЫЕ КОМАНДЫ =====

    /// <summary>
    /// Голосовая команда: найти ближайшую цель
    /// </summary>
    public void VoiceCommand_FindNearestTarget()
    {
        var target = DetectTargetAuth();
        if (target != null)
        {
            Debug.Log($"🎯 Ближайшая цель: {target.name}");
            PlayDetectionSound();
        }
        else
        {
            Debug.Log($"🔍 Цели не обнаружены");
        }
    }

    /// <summary>
    /// Голосовая команда: сканировать область
    /// </summary>
    public void VoiceCommand_ScanArea()
    {
        var targets = DetectAllTargets();
        Debug.Log($"🔍 Обнаружено целей: {targets.Length}");
        
        foreach (var target in targets)
        {
            var unitDef = target.GetComponent<UnitsDefinition>();
            string unitName = unitDef != null ? unitDef.GetUnitName() : "Неизвестно";
            Debug.Log($"  - {unitName} на расстоянии {Vector2.Distance(transform.position, target.position):F1}м");
        }
    }

    /// <summary>
    /// Голосовая команда: изменить радиус обзора
    /// </summary>
    public void VoiceCommand_IncreaseRange()
    {
        SetViewRadius(viewRadius + 2f);
    }

    /// <summary>
    /// Голосовая команда: уменьшить радиус обзора
    /// </summary>
    public void VoiceCommand_DecreaseRange()
    {
        SetViewRadius(viewRadius - 2f);
    }

    // ===== ОТЛАДКА =====

    /// <summary>
    /// Включение/отключение голосовых отчетов
    /// </summary>
    public void SetVoiceReporting(bool enabled)
    {
        enableVoiceReporting = enabled;
        Debug.Log($"🎙️ Голосовые отчеты детектора {gameObject.name}: {(enabled ? "включены" : "отключены")}");
    }

    /// <summary>
    /// Получение статистики детектора
    /// </summary>
    public string GetDetectorStats()
    {
        var targets = DetectAllTargets();
        return $"Радиус: {viewRadius}, Угол: {viewAngle}°, Целей: {targets.Length}, Может атаковать: {canHit}";
    }

    // ===== ВИЗУАЛИЗАЦИЯ В РЕДАКТОРЕ =====

    void OnDrawGizmosSelected()
    {
        // Радиус обзора - используем DrawWireSphere для совместимости
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Угол обзора
        if (viewAngle < 360f)
        {
            Vector3 forward = transform.up;
            float halfAngle = viewAngle * 0.5f;
            
            Vector3 leftBoundary = Quaternion.Euler(0, 0, halfAngle) * forward * viewRadius;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, -halfAngle) * forward * viewRadius;
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        }

        // Обнаруженные цели
        if (Application.isPlaying)
        {
            var targets = DetectAllTargets();
            Gizmos.color = Color.red;
            foreach (var target in targets)
            {
                Gizmos.DrawLine(transform.position, target.position);
                Gizmos.DrawWireCube(target.position, Vector3.one * 0.5f);
            }
        }
    }
}