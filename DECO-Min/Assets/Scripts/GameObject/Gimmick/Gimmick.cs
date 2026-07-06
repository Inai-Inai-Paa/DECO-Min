using Unity.VisualScripting;
using UnityEngine;

public class Gimmick : MonoBehaviour
{
    protected StateMachine _stateMachine;
    public Player player;

    [SerializeField]
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

    public void ChangeState(GimmickState state)
    {
        GimmickState stateInstance = Instantiate(state);
        stateInstance.Initialize(this, _stateMachine);
		_stateMachine.ChangeState(stateInstance);
        
	}
}
