using UnityEngine;

[CreateAssetMenu(menuName = "State/PlayerMoveState")]
public class PlayerMove : State
{
    public float moveSpeed = 3.0f;

    PlayerMove()
    {
        stateName = "PlayerMove";
    }

    public override void Enter()
    {
        Debug.Log("PlayerMove Enter");
    }
}
