using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Return")]
public class RangedEnemyReturn : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _idleState;
    [SerializeField] private EnemyState _attackState;

    public override void Enter()
    {
        rangedEnemy.OnReturnStateEntered();
        rangedEnemy.EndDirectMovement();
        rangedEnemy.StopMove();
    }

    public override void Update()
    {
        if (rangedEnemy.Lifecycle == EnemyLifecycle.WaitDespawn
            || rangedEnemy.Lifecycle == EnemyLifecycle.Inactive
            || rangedEnemy.Lifecycle == EnemyLifecycle.Dead)
        {
            rangedEnemy.StopMove();
            return;
        }

        rangedEnemy.StopMove();

        if (CanResumeCombat())
        {
            if (rangedEnemy.IsTargetInAttackRange() && rangedEnemy.HasLineOfSightToTarget())
            {
                ChangeRangedState<RangedEnemyAttack>(_attackState);
            }
            else
            {
                ChangeRangedState<RangedEnemyIdle>(_idleState);
            }

            return;
        }

        if (rangedEnemy.HasLeash)
        {
            rangedEnemy.CompleteLeashReturn();
            return;
        }

        ChangeRangedState<RangedEnemyIdle>(_idleState);
    }

    private bool CanResumeCombat()
    {
        if (rangedEnemy.HasLeash)
        {
            if (!rangedEnemy.CanResumeFromLeashReturn())
            {
                return false;
            }

            rangedEnemy.MarkCombatResumed();
            return true;
        }

        return rangedEnemy.HasTarget()
            && rangedEnemy.IsTargetWithinHomeChaseDistance()
            && rangedEnemy.Awareness == EnemyAwareness.Engaged;
    }
}
