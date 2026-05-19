using UnityEngine;

[CreateAssetMenu(menuName = "State/Player/Locomotion/Jump")]
public class PlayerJump : PlayerLocomotionState
{
    public float jumpForce = 5.0f;

    [SerializeField]
    private PlayerLocomotionState moveState;
    public override void Enter()
    {
        player.baseVelocity = new Vector3(player.baseVelocity.x, jumpForce, player.baseVelocity.z);
    }

    public override void Update()
    {

    }
}
