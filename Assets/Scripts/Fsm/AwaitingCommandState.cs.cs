// Assets/Scripts/Fsm/AwaitingCommandState.cs

using UnityEngine;

public class AwaitingCommandState : BaseState
{
    public AwaitingCommandState(UnitFSM fsm) : base(fsm) { }

    public override void OnEnter()
    {
        Debug.Log($"{unit.name}: 🕒 Жду команду...");
        fsm.PlayRandomResponse();
    }

    public override void OnUpdate()
    {
        // Ничего не делаем — ждём команду
    }
}