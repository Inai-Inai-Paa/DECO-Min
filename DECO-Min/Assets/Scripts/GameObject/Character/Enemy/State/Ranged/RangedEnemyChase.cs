using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Chase")]
public class RangedEnemyChase : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _idleState;
    [SerializeField] private EnemyState _attackState;

    public override void Enter()
    {
        rangedEnemy.StopMove();
    }

    public override void Update()
    {
        rangedEnemy.StopMove();

        if (rangedEnemy.IsTargetInAttackRange() && rangedEnemy.HasLineOfSightToTarget())
        {
            ChangeRangedState<RangedEnemyAttack>(_attackState);
            return;
        }

        ChangeRangedState<RangedEnemyIdle>(_idleState);
    }
}
