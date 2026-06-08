using Unity.Collections;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Status")]
    [SerializeField]
    protected CharacterStatus characterStatus;

    public Vector3 velocity { get; protected set; }
    [HideInInspector] public Vector3 baseVelocity;
    [HideInInspector] public Vector3 additionalVelocity;

    protected StateMachine stateMachine;

    private Rigidbody rb;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();

        stateMachine = new StateMachine();
    }

    protected virtual void Update()
    {
        baseVelocity = rb.linearVelocity;
        additionalVelocity = Vector3.zero;

        stateMachine.Update();

        velocity = baseVelocity + additionalVelocity;
        rb.linearVelocity = velocity;
    }
    protected virtual void FixedUpdate()
    {
        baseVelocity = rb.linearVelocity;
        additionalVelocity = Vector3.zero;

        stateMachine.FixedUpdate();

        velocity = baseVelocity + additionalVelocity;
        rb.linearVelocity = velocity;
    }
}
