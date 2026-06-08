using UnityEngine;

public abstract class EnemyState : State
{
    // 参照用のエネミーとステートマシン
    protected Enemy enemy;
    protected StateMachine stateMachine;

    /// <summary>
    /// 初期化関数
    /// </summary>
    /// <param name="enemy">参照用のエネミー</param>
    /// <param name="stateMachine">参照用のステートマシン</param>
    public void Initialize(Enemy enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }
}