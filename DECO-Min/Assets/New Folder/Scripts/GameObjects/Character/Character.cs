using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    private float maxHP = 100.0f;

    public float HP { get; protected set; }

    public Vector3 velocity { get; protected set; }
    public Vector3 baseVelocity;
    public Vector3 additionalVelocity;

    protected StateMachine locomotionStateMachine;
    protected StateMachine combatStateMachine;

    private Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

        locomotionStateMachine = new StateMachine();
        combatStateMachine = new StateMachine();

        HP = maxHP;
    }

    protected virtual void Update()
    {
        baseVelocity = rb.linearVelocity;
        additionalVelocity = Vector3.zero;

        locomotionStateMachine.Update();
        combatStateMachine.Update();

        velocity = baseVelocity + additionalVelocity;
    }
    protected virtual void FixedUpdate()
    {
        rb.linearVelocity = velocity;
    }
}
