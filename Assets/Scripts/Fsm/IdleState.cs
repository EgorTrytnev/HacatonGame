using UnityEngine;

/// <summary>
/// Состояние ожидания - базовое состояние юнита
/// Юнит стоит на месте и ждет команд
/// </summary>
public class IdleState : BaseState
{
    private float idleTime = 0f;
    private float maxIdleTime = 5f;

    public IdleState(UnitFSM fsm) : base(fsm) { }

    public override void OnEnter()
    {
        Debug.Log($"🏃 {unit.name}: В режиме ожидания");
        idleTime = 0f;
    }

    public override void OnUpdate()
    {
        idleTime += Time.deltaTime;

        // Периодически проигрываем звук ожидания
        if (idleTime >= maxIdleTime)
        {
            idleTime = 0f;
            if (fsm != null && Random.Range(0f, 1f) < 0.3f) // 30% шанс
            {
                fsm.PlayRandomResponse();
            }
        }
    }

    public override void OnExit()
    {
        Debug.Log($"✋ {unit.name}: Выходим из режима ожидания");
    }

    public override bool CanBeInterrupted()
    {
        return true; // Состояние ожидания всегда может быть прервано
    }

    public override int GetPriority()
    {
        return 1; // Низкий приоритет
    }
}