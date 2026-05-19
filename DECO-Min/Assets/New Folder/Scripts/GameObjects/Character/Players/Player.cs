using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEngine.PlayerLoop.PostLateUpdate;

public partial class Player : Character
{
    public PlayerInputData playerInputData;

    [SerializeField]
    private PlayerLocomotionState initLocomotionState = null;

    [SerializeField]
    private PlayerCombatState initCombatState = null;

    protected override void Awake()
    {
        // Call the base class's Awake method to ensure that the state machine is initialized
        base.Awake();

        InitializeInput();

        if (initLocomotionState != null)
        {
            ChangePlayerState(locomotionStateMachine, initLocomotionState);
        }
        if (initCombatState != null)
        {
            ChangePlayerState(combatStateMachine, initCombatState);
        }
    }
    private void OnDestroy()
    {
        FinalizeInput();
    }

    protected override void Update()
    {
        base.Update();
    }

    public void ChangeLocomotionState(
    PlayerLocomotionState state)
    {
        ChangePlayerState(
            locomotionStateMachine,
            state);
    }

    public void ChangeCombatState(
        PlayerCombatState state)
    {
        ChangePlayerState(
            combatStateMachine,
            state);
    }
    private void ChangePlayerState<T>(
        StateMachine stateMachine,
        T state)
        where T : PlayerState
    {
        T prevState = stateMachine.GetState<T>();

        T newState = Instantiate(state);

        newState.Initialize(this, stateMachine);

        stateMachine.ChangeState(newState);

        if (prevState != null)
        {
            Destroy(prevState);
        }
    }
};