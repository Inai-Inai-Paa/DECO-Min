using UnityEngine;

public abstract class PlayerState : State
{
    // 参照用のプレイヤーとステートマシン
    protected Player player;
    protected StateMachine stateMachine;

    /// <summary>
    /// 初期化関数
    /// </summary>
    /// <param name="player">参照用のプレイヤー</param>
    /// <param name="stateMachine">参照用のステートマシン</param>
    public void Initialize(Player player, StateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }
}
public abstract class PlayerLocomotionState : PlayerState
{
}
public abstract class PlayerCombatState : PlayerState
{
}
