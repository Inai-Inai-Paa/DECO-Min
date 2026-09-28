using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Alert")]
public class RangedEnemyAlert : RangedEnemyState
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
        rangedEnemy.LookAtTargetForCombat();

        if (rangedEnemy.Awareness == EnemyAwareness.Unaware)
        {
            ChangeRangedState<RangedEnemyIdle>(_idleState);
            return;
        }

        if (rangedEnemy.Awareness == EnemyAwareness.Engaged
            && rangedEnemy.IsTargetInAttackRange()
            && rangedEnemy.HasLineOfSightToTarget())
        {
            ChangeRangedState<RangedEnemyAttack>(_attackState);
        }
    }
}
