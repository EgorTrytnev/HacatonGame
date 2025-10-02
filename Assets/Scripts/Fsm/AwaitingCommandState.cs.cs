using UnityEngine;

/// <summary>
/// Состояние движения к определенной точке
/// Используется для голосовых команд типа "иди к базе"
/// </summary>
public class MoveToPointState : BaseState
{
    private Vector3 targetPosition;
    private float speed = 3f;
    private string pointName;
    private float stuckTimer = 0f;
    private float maxStuckTime = 3f;
    private Vector3 lastPosition;
    private bool useRigidbody = false;
    private Rigidbody2D rb2d;

    public MoveToPointState(UnitFSM fsm, Vector3 target, string name) : base(fsm)
    {
        targetPosition = target;
        pointName = name;
    }

    public override void OnEnter()
    {
        Debug.Log($"🚶 {unit.name}: Двигаюсь к точке '{pointName}'");
        lastPosition = unit.transform.position;
        stuckTimer = 0f;

        // Проверяем, есть ли Rigidbody2D для физического движения
        rb2d = unit.GetComponent<Rigidbody2D>();
        useRigidbody = rb2d != null;

        // Воспроизводим звук подтверждения
        if (fsm != null)
        {
            fsm.PlayRandomResponse();
        }
    }

    public override void OnUpdate()
    {
        // Проверяем достижение цели
        float distanceToTarget = Vector3.Distance(unit.transform.position, targetPosition);
        
        if (distanceToTarget < 0.5f) // Увеличиваем порог для более стабильной работы
        {
            // Достиг цели
            unit.transform.position = new Vector3(targetPosition.x, targetPosition.y, unit.transform.position.z);
            Debug.Log($"✅ {unit.name}: Достиг точки '{pointName}'");
            
            // Воспроизводим звук завершения
            if (fsm != null)
            {
                fsm.PlayRandomResponse();
            }
            
            fsm.TransitionTo(new IdleState(fsm));
            return;
        }

        // Движение к цели
        Vector3 direction = (targetPosition - unit.transform.position).normalized;
        
        if (useRigidbody)
        {
            // Физическое движение через Rigidbody2D
            Vector2 movement = new Vector2(direction.x, direction.y) * speed;
            rb2d.linearVelocity = movement;
        }
        else
        {
            // Прямое движение через Transform
            unit.transform.position += direction * speed * Time.deltaTime;
        }

        // Поворот юнита в сторону движения (опционально)
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            unit.transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }

        // Проверка на застревание
        CheckForStuck();
    }

    public override void OnExit()
    {
        // Останавливаем движение при выходе из состояния
        if (useRigidbody && rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
        }
        
        Debug.Log($"🛑 {unit.name}: Прекращаю движение к '{pointName}'");
    }

    /// <summary>
    /// Проверка на застревание юнита
    /// </summary>
    private void CheckForStuck()
    {
        float distanceMoved = Vector3.Distance(unit.transform.position, lastPosition);
        
        if (distanceMoved < 0.1f) // Почти не двигается
        {
            stuckTimer += Time.deltaTime;
            
            if (stuckTimer >= maxStuckTime)
            {
                Debug.LogWarning($"⚠️ {unit.name}: Застрял при движении к '{pointName}', возвращаюсь в режим ожидания");
                fsm.TransitionTo(new IdleState(fsm));
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = unit.transform.position;
        }
    }

    public override bool CanBeInterrupted()
    {
        return true; // Движение может быть прервано новой командой
    }

    public override int GetPriority()
    {
        return 5; // Средний приоритет
    }

    /// <summary>
    /// Получить текущую цель движения
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }

    /// <summary>
    /// Получить имя точки назначения
    /// </summary>
    public string GetPointName()
    {
        return pointName;
    }

    /// <summary>
    /// Установить скорость движения
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0.1f, newSpeed);
    }

    /// <summary>
    /// Получить прогресс движения (0-1)
    /// </summary>
    public float GetProgress()
    {
        Vector3 startPos = lastPosition;
        float totalDistance = Vector3.Distance(startPos, targetPosition);
        float currentDistance = Vector3.Distance(unit.transform.position, targetPosition);
        
        return totalDistance > 0 ? Mathf.Clamp01(1f - (currentDistance / totalDistance)) : 1f;
    }
}