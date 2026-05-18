using JetBrains.Annotations;
using UnityEngine;

public class Player : Character
{
    [SerializeField]
    private State initState = null;

    public Player()
    {
        // StateMachine is initialized in Character constructor
        stateMachine.ChangeState(initState);
    }

    public override void Update()
    {
        stateMachine.Update();
    }
};
