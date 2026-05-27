using Unity.Collections;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("パラメータ")]
    [Space(2)]
    [Tooltip("最大HPです。")]
    [SerializeField]
    private float maxHP = 100.0f;
    [Tooltip("現在のHPです。起動時は最大HPと同じ値になります。")]
    public float HP = 100.0f;

    public Vector3 velocity { get; protected set; }
    [HideInInspector] public Vector3 baseVelocity;
    [HideInInspector] public Vector3 additionalVelocity;

    protected StateMachine stateMachine;

    private Rigidbody rb;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();

        stateMachine = new StateMachine();

        HP = maxHP;
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
