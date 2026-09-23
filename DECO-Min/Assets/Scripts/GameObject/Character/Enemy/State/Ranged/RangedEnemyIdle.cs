using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Idle")]
public class RangedEnemyIdle : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _alertState;
    [SerializeField] private EnemyState _chaseState;
    [SerializeField] private EnemyState _attackState;

    public override void Enter()
    {
        rangedEnemy.StopMove();
    }

    public override void Update()
    {
        if (!rangedEnemy.IsTargetWithinHomeChaseDistance())
        {
            ChangeRangedState<RangedEnemyReturn>(null);
            return;
        }

        if (rangedEnemy.Awareness == EnemyAwareness.Engaged)
        {
            if (rangedEnemy.IsTargetInAttackRange() && rangedEnemy.HasLineOfSightToTarget())
            {
                ChangeRangedState<RangedEnemyAttack>(_attackState);
            }
            else
            {
                ChangeRangedState<RangedEnemyChase>(_chaseState);
            }

            return;
        }

        if (rangedEnemy.Awareness == EnemyAwareness.Alert)
        {
            ChangeRangedState<RangedEnemyAlert>(_alertState);
        }
    }
}
