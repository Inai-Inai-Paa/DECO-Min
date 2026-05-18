using UnityEngine;

public class Character : MonoBehaviour
{
    public StateMachine stateMachine = null;

    public float HP = 100.0f;

    public Character()
    {
        stateMachine = new StateMachine();
    }

    public virtual void Update()
    {
        stateMachine.Update();
    }
}
