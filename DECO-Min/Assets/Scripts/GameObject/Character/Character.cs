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
        bool driveBody = rb != null && !rb.isKinematic;

        if (driveBody)
        {
            baseVelocity = rb.linearVelocity;
        }

        additionalVelocity = Vector3.zero;
        stateMachine.Update();
        velocity = baseVelocity + additionalVelocity;

        if (driveBody)
        {
            rb.linearVelocity = velocity;
        }
    }

    protected virtual void FixedUpdate()
    {
        bool driveBody = rb != null && !rb.isKinematic;

        if (driveBody)
        {
            baseVelocity = rb.linearVelocity;
        }

        additionalVelocity = Vector3.zero;
        stateMachine.FixedUpdate();
        velocity = baseVelocity + additionalVelocity;

        if (driveBody)
        {
            rb.linearVelocity = velocity;
        }
    }
}
