using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Chase")]
public class RangedEnemyChase : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _idleState;
    [SerializeField] private EnemyState _attackState;

    public override void Enter()
    {
        rangedEnemy.ResumeMove();
    }

    public override void Update()
    {
        if (!rangedEnemy.IsTargetWithinHomeChaseDistance())
        {
            ChangeRangedState<RangedEnemyReturn>(null);
            return;
        }

        if (!rangedEnemy.HasTarget() || rangedEnemy.Awareness == EnemyAwareness.Unaware)
        {
            ChangeRangedState<RangedEnemyIdle>(_idleState);
            return;
        }

        if (rangedEnemy.IsTargetInAttackRange() && rangedEnemy.HasLineOfSightToTarget())
        {
            ChangeRangedState<RangedEnemyAttack>(_attackState);
            return;
        }

        rangedEnemy.SetTargetDestination();
    }
}
