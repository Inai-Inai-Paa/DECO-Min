using Unity.VisualScripting;
using UnityEngine;

public class Gimmick : MonoBehaviour
{
    protected StateMachine _stateMachine;
    protected Player _player;
    protected GimmickActiveState _activeState;

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

    protected virtual void Active()
    {

        _activeState.Initialize(this, _stateMachine);
        _stateMachine.ChangeState(Instantiate(_activeState));
    }
}
