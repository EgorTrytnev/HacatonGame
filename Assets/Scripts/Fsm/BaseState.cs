using UnityEngine;

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
}