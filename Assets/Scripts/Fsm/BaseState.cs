using UnityEngine;

/// <summary>
/// Базовый класс для всех состояний FSM юнитов
/// Интегрированный с системой голосового управления
/// </summary>
public abstract class BaseState
{
    protected UnitFSM fsm;
    protected GameObject unit;

    public BaseState(UnitFSM fsm)
    {
        this.fsm = fsm;
        this.unit = fsm.gameObject;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }

    /// <summary>
    /// Получить имя состояния для отладки
    /// </summary>
    public virtual string GetStateName()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Проверить, может ли состояние быть прервано голосовой командой
    /// </summary>
    public virtual bool CanBeInterrupted()
    {
        return true;
    }

    /// <summary>
    /// Получить приоритет состояния (для разрешения конфликтов)
    /// </summary>
    public virtual int GetPriority()
    {
        return 0; // Базовый приоритет
    }
}