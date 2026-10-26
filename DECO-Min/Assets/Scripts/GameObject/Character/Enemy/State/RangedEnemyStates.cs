using UnityEngine;

public abstract class RangedEnemyState : EnemyState
{
    protected RangedEnemy rangedEnemy => enemy as RangedEnemy;
}

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
            ChangeTo<RangedEnemyAttack>(_attackState);
            return;
        }

        if (rangedEnemy.Awareness == EnemyAwareness.Alert
            || rangedEnemy.Awareness == EnemyAwareness.Engaged)
        {
            ChangeTo<RangedEnemyAlert>(_alertState);
        }
    }
}

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
            ChangeTo<RangedEnemyIdle>(_idleState);
            return;
        }

        if (rangedEnemy.Awareness == EnemyAwareness.Engaged
            && rangedEnemy.IsTargetInAttackRange()
            && rangedEnemy.HasLineOfSightToTarget())
        {
            ChangeTo<RangedEnemyAttack>(_attackState);
        }
    }
}

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
            ChangeTo<RangedEnemyAttack>(_attackState);
            return;
        }

        ChangeTo<RangedEnemyIdle>(_idleState);
    }
}

public class RangedEnemyAttack : RangedEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _idleState;
    [SerializeField] private EnemyState _alertState;

    public override void Enter()
    {
        rangedEnemy.StopMove();
        rangedEnemy.BeginPreparation();
    }

    public override void Exit() => rangedEnemy.CancelPreparation();

    public override void Update()
    {
        if (rangedEnemy.IsDead || rangedEnemy.IsDespawning || rangedEnemy.IsDown || rangedEnemy.IsCombatAIFrozen)
        {
            rangedEnemy.CancelPreparation();
            return;
        }

        if (rangedEnemy.IsPreparing)
        {
            rangedEnemy.TickPreparation(Time.deltaTime);
            return;
        }

        if (!rangedEnemy.HasTarget() || rangedEnemy.Awareness == EnemyAwareness.Unaware)
        {
            ChangeTo<RangedEnemyIdle>(_idleState);
            return;
        }

        if (!rangedEnemy.IsTargetInAttackRange() || !rangedEnemy.HasLineOfSightToTarget())
        {
            ChangeTo<RangedEnemyAlert>(_alertState);
            return;
        }

        rangedEnemy.StopMove();
        if (!rangedEnemy.IsPreparing) rangedEnemy.BeginPreparation();
        rangedEnemy.TickPreparation(Time.deltaTime);
    }
}

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
                ChangeTo<RangedEnemyAttack>(_attackState);
            }
            else
            {
                ChangeTo<RangedEnemyIdle>(_idleState);
            }

            return;
        }

        if (rangedEnemy.HasLeash)
        {
            rangedEnemy.CompleteLeashReturn();
            return;
        }

        ChangeTo<RangedEnemyIdle>(_idleState);
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

public class RangedEnemyDown : RangedEnemyState
{
    private float _downTimer;

    public override void Enter()
    {
        rangedEnemy.StopMove();
        _downTimer = 0.0f;
    }

    public override void Update()
    {
        rangedEnemy.StopMove();
        _downTimer += Time.deltaTime;

        if (_downTimer >= rangedEnemy.DownRecoverTime)
        {
            rangedEnemy.RecoverFromDownForState();
        }
    }
}
