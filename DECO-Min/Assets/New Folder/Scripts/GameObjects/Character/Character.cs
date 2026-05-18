using UnityEngine;

public class Character : MonoBehaviour
{

    [SerializeField] 
    private float maxHP = 100.0f;
    
    public StateMachine stateMachine = null;
    public float HP = 100.0f;

    public Character()
    {
        stateMachine = new StateMachine();
        HP = maxHP;
    }

    public virtual void Update()
    {
        stateMachine.Update();
    }
}
