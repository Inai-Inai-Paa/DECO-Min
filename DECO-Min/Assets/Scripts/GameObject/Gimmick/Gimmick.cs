using UnityEngine;

public class Gimmick : MonoBehaviour
{
    protected StateMachine _stateMachine;
    protected Player _player;

    protected virtual void Start()
    {
        _stateMachine = new StateMachine();
    }

    protected virtual void Update()
    {
        _stateMachine.Update();
    }
    protected virtual void FixedUpdate()
    {
        _stateMachine.FixedUpdate();
    }

    public virtual void ChangeGimmickState(GimmickState state)
    {
        GimmickState nextState = Instantiate(state);
        nextState.Initialize(this, _stateMachine);
        _stateMachine.ChangeState(nextState);
    }
}
