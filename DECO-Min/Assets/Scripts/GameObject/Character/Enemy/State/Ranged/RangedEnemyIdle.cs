using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Idle")]
public class RangedEnemyIdle : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _alertState;
    [SerializeField] private EnemyState _attackState;

    public override void Enter()
    {
        rangedEnemy.StopMove();
    }

    public override void Update()
    {
        rangedEnemy.StopMove();

        if (rangedEnemy.Awareness == EnemyAwareness.Engaged
            && rangedEnemy.IsTargetInAttackRange()
            && rangedEnemy.HasLineOfSightToTarget())
        {
            ChangeRangedState<RangedEnemyAttack>(_attackState);
            return;
        }

        if (rangedEnemy.Awareness == EnemyAwareness.Alert
            || rangedEnemy.Awareness == EnemyAwareness.Engaged)
        {
            ChangeRangedState<RangedEnemyAlert>(_alertState);
        }
    }
}
