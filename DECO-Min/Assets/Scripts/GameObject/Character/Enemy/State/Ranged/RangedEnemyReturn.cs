using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Return")]
public class RangedEnemyReturn : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _idleState;
    [SerializeField] private EnemyState _chaseState;
    [SerializeField] private EnemyState _attackState;

    private float _returnTimer;
    private float _homeWaitTimer;
    private int _returnRetryCount;
    private bool _isWaitingAtHome;

    public override void Enter()
    {
        rangedEnemy.OnReturnStateEntered();
        rangedEnemy.EndDirectMovement();
        rangedEnemy.ResumeMove();
        rangedEnemy.SetHomeDestination();
        _returnTimer = 0.0f;
        _homeWaitTimer = 0.0f;
        _returnRetryCount = 0;
        _isWaitingAtHome = false;
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

        if (CanResumeCombat())
        {
            if (rangedEnemy.IsTargetInAttackRange())
            {
                ChangeRangedState<RangedEnemyAttack>(_attackState);
            }
            else
            {
                ChangeRangedState<RangedEnemyChase>(_chaseState);
            }

            return;
        }

        if (_isWaitingAtHome)
        {
            UpdateHomeWait();
            return;
        }

        _returnTimer += Time.deltaTime;

        if (rangedEnemy.HasLeash && rangedEnemy.IsWithinReturnCompleteDistance())
        {
            rangedEnemy.CompleteLeashReturn();
            return;
        }

        if (rangedEnemy.IsAtHome() || rangedEnemy.IsArrived())
        {
            if (rangedEnemy.HasLeash)
            {
                rangedEnemy.CompleteLeashReturn();
                return;
            }

            BeginHomeWait();
            return;
        }

        if (_returnTimer >= rangedEnemy.ReturnImpossibleTime)
        {
            if (_returnRetryCount == 0 && rangedEnemy.TryCorrectToNearbyNavMeshPosition())
            {
                rangedEnemy.SetHomeDestination();
                _returnTimer = 0.0f;
                _returnRetryCount++;
                return;
            }

            if (rangedEnemy.HasLeash)
            {
                rangedEnemy.SetHomeDestination();
                return;
            }

            rangedEnemy.Despawn();
            return;
        }

        rangedEnemy.SetHomeDestination();
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

    private void BeginHomeWait()
    {
        _isWaitingAtHome = true;
        _homeWaitTimer = 0.0f;
        rangedEnemy.StopMove();
    }

    private void UpdateHomeWait()
    {
        if (rangedEnemy.IsTargetInSpawnArea())
        {
            ChangeRangedState<RangedEnemyIdle>(_idleState);
            return;
        }

        _homeWaitTimer += Time.deltaTime;

        if (_homeWaitTimer >= rangedEnemy.HomeDespawnWaitTime)
        {
            rangedEnemy.Despawn();
        }
    }
}
