using UnityEngine;

public class GimmickState : State
{
    // 参照用のプレイヤーとステートマシン
    protected Gimmick gimmick;
    protected StateMachine stateMachine;

    /// <summary>
    /// 初期化関数
    /// </summary>
    /// <param name="gimmick">参照用のプレイヤー</param>
    /// <param name="stateMachine">参照用のステートマシン</param>
    public void Initialize(Gimmick gimmick, StateMachine stateMachine)
    {
        this.gimmick = gimmick;
        this.stateMachine = stateMachine;
    }
}
