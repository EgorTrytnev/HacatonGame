// Assets/Scripts/Fsm/MoveToPointState.cs
using UnityEngine;

public class MoveToPointState : BaseState
{
    private Vector3 targetPosition;
    private float speed = 3f;
    private string pointName;

    public MoveToPointState(UnitFSM fsm, Vector3 target, string name) : base(fsm)
    {
        targetPosition = target;
        pointName = name;
    }

    public override void OnEnter()
    {
        Debug.Log($"{unit.name}: Двигаюсь к точке '{pointName}'");
    }

    public override void OnUpdate()
    {
        if (Vector3.Distance(unit.transform.position, targetPosition) < 0.1f)
        {
            // Достиг цели
            unit.transform.position = targetPosition;
            Debug.Log($"{unit.name}: Достиг точки '{pointName}'");
            fsm.TransitionTo(new IdleState(fsm));
            return;
        }

        Vector3 direction = (targetPosition - unit.transform.position).normalized;
        unit.transform.position += direction * speed * Time.deltaTime;
        // Поворачиваем юнит в сторону движения (опционально)
        // if (direction != Vector3.zero)
        //     unit.transform.forward = direction;
    }
}