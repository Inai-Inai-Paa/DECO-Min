using UnityEngine;

public abstract class State : ScriptableObject
{
    public string stateName;

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
}

public class StateMachine
{
    private State currentState;

    public void ChangeState(State newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.Enter();
        }
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    public State GetState()
    {
        return currentState;
    }
}