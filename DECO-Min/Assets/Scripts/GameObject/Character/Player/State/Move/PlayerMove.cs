using UnityEngine;

[CreateAssetMenu(menuName = "State/Player/Move")]
public class PlayerMove : PlayerState
{
    [Header("TransitionState")]
    [Tooltip("PeelState"),SerializeField] private PlayerState peelState;

    [Header("�ړ����x")]
    [Tooltip("�ړ����x�ł��B")]
    public float moveSpeed = 3.0f;

    [Header("�W�����v��")]
    [Tooltip("�W�����v�͂ł��B")]
    public float jumpForce = 5.0f;
    
    [SerializeField] private PlayerState _attackState;

    public override void Update()
    {
        if (player.playerInputData.AttackPressed)
            player.ChangePlayerState(_attackState);

            if(player.playerInputData.PealPressed)
        {
            stateMachine.ChangeState(peelState);
        }
    }

    public override void FixedUpdate()
    {
        Move();

        if (player.isGrounded && player.playerInputData.HasJumpBuffered(0.05f))
        {
            player.playerInputData.JumpPressedTime = -Mathf.Infinity;
            player.baseVelocity = new Vector3(player.baseVelocity.x, jumpForce, player.baseVelocity.z);
        }
    }

    private void Move()
    {
        Vector3 input =
            new Vector3(
                player.playerInputData.Move.x,
                0.0f,
                player.playerInputData.Move.y);

        input.Normalize();

        Vector3 velocity =
            (input.z * player.cameraForward + input.x * Vector3.Cross(Vector3.up, player.cameraForward)) * moveSpeed;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            player.transform.rotation = Quaternion.Lerp(
                player.transform.rotation,
                Quaternion.LookRotation(velocity, Vector3.up),
                Time.fixedDeltaTime * moveSpeed * input.magnitude
            );
        }
        player.baseVelocity = new Vector3(velocity.x, player.baseVelocity.y, velocity.z);
    }
}
