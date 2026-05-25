using UnityEngine;

[CreateAssetMenu(menuName = "State/Player/Locomotion/Move")]
public class PlayerMove : PlayerLocomotionState
{
    public float moveSpeed = 3.0f;

    [SerializeField]
    private PlayerLocomotionState jumpState;

    public override void Update()
    {
        Move();

        if (player.playerInputData.HasJumpBuffered(0.2f))
        {
            player.ChangeLocomotionState(jumpState);
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
            input * moveSpeed;

        player.baseVelocity = new Vector3(velocity.x, player.baseVelocity.y, velocity.z);
    }
}
