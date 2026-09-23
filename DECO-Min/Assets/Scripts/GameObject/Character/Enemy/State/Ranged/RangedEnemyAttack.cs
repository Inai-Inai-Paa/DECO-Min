using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Attack")]
public class RangedEnemyAttack : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _idleState;
    [SerializeField] private EnemyState _chaseState;

    public override void Enter()
    {
        rangedEnemy.StopMove();
        rangedEnemy.LookAtTargetForCombat();
        rangedEnemy.TryFireBullet();
        rangedEnemy.ResetAttackCooldown();
    }

    public override void Update()
    {
        if (rangedEnemy.IsDead || rangedEnemy.IsDespawning || rangedEnemy.IsDown)
        {
            return;
        }

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

        if (!rangedEnemy.IsTargetInAttackRange() || !rangedEnemy.HasLineOfSightToTarget())
        {
            ChangeRangedState<RangedEnemyChase>(_chaseState);
            return;
        }

        rangedEnemy.StopMove();
        rangedEnemy.LookAtTargetForCombat();

        if (!rangedEnemy.UpdateAttackCooldown())
        {
            return;
        }

        rangedEnemy.TryFireBullet();
        rangedEnemy.ResetAttackCooldown();
    }
}
