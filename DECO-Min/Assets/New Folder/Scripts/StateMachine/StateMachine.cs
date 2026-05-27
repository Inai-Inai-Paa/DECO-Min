using Unity.VisualScripting;
using UnityEngine;

public abstract class State : ScriptableObject
{
    public string stateName { get; protected set; }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}

public class StateMachine
{
    private State currentState;

    public void ChangeState(State newState)
    {
        currentState?.Exit();

        currentState = newState;

        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public T GetState<T>()
        where T : State
    {
        return currentState as T;
    }
}