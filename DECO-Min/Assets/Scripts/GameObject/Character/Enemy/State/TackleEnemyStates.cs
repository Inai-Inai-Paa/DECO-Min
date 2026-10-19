using UnityEngine;

public abstract class TackleEnemyState : EnemyState
{
    protected TackleEnemy tackleEnemy => enemy as TackleEnemy;
}

public class TackleEnemyPatrol : TackleEnemyState
{
    private enum Phase
    {
        Wait,
        Turn,
        Move
    }

    [Header("TransitionState")]
    [SerializeField] private EnemyState _alertState;

    private Phase _phase;
    private float _timer;
    private Vector3 _destination;
    private Quaternion _turnStart;
    private Quaternion _turnEnd;

    public override void Enter()
    {
        tackleEnemy.ClearAwarenessForState();
        tackleEnemy.ReleasePlayerBodyCollision();
        tackleEnemy.ApplyPatrolSpeed();
        tackleEnemy.SetAgentRotationUpdates(false);
        tackleEnemy.StopMoveForState();
        _timer = 0.0f;
        _phase = Phase.Wait;

        if (!tackleEnemy.TryChoosePatrolDestination(out _destination))
        {
            _destination = tackleEnemy.transform.position;
        }
    }

    public override void Exit()
    {
        tackleEnemy.SetAgentRotationUpdates(true);
    }

    public override void Update()
    {
        if (tackleEnemy.IsTargetVisibleForTackle())
        {
            if (tackleEnemy._animator != null)
            {
                tackleEnemy._animator.SetTrigger("Find");
            }
            ChangeTo<TackleEnemyAlert>(_alertState);
            return;
        }

        switch (_phase)
        {
            case Phase.Wait:
                tackleEnemy.StopMoveForState();
                _timer += Time.deltaTime;

                if (_timer >= tackleEnemy.PatrolTurnWait)
                {
                    BeginTurn();
                }

                break;

            case Phase.Turn:
                _timer += Time.deltaTime;
                float turn = Mathf.Clamp01(_timer / tackleEnemy.PatrolTurnDuration);
                tackleEnemy.transform.rotation = Quaternion.Slerp(_turnStart, _turnEnd, turn);

                if (turn >= 1.0f)
                {
                    tackleEnemy.BeginPatrolMove(_destination);
                    _phase = Phase.Move;
                }

                break;

            case Phase.Move:
                if (tackleEnemy.HasReachedPatrolPoint(_destination))
                {
                    tackleEnemy.StopMoveForState();

                    if (!tackleEnemy.TryChoosePatrolDestination(out _destination))
                    {
                        _destination = tackleEnemy.transform.position;
                    }

                    _timer = 0.0f;
                    _phase = Phase.Wait;
                }

                break;
        }
    }

    private void BeginTurn()
    {
        _phase = Phase.Turn;
        _timer = 0.0f;
        _turnStart = tackleEnemy.transform.rotation;
        Vector3 direction = _destination - tackleEnemy.transform.position;
        direction.y = 0.0f;
        _turnEnd = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized)
            : _turnStart;
    }
}

public class TackleEnemyAlert : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _chargeState;

    private float _aimTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        tackleEnemy.ClearLockedTackleDirection();
        tackleEnemy.RememberVisibleAimPoint();
        _aimTimer = 0.0f;
    }

    public override void Update()
    {
        tackleEnemy.RememberVisibleAimPoint();

        if (tackleEnemy.HasTargetForState() && tackleEnemy.HasLineOfSightToTarget())
        {
            tackleEnemy.LookAtTargetForState();
        }
        else if (tackleEnemy.HasAimPoint)
        {
            tackleEnemy.LookAtHorizontalPoint(tackleEnemy.AimPoint);
        }

        _aimTimer += Time.deltaTime;

        if (_aimTimer < tackleEnemy.AimFollowTime)
        {
            return;
        }

        tackleEnemy.LockTackleAim();

        if (tackleEnemy._animator != null)
        {
            tackleEnemy._animator.SetTrigger("Charge");
        }
        ChangeTo<TackleEnemyCharge>(_chargeState);
    }
}

public class TackleEnemyChase : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _chargeState;

    public override void Enter()
    {
        tackleEnemy.ResumeMoveForState();
    }

    public override void Update()
    {
        if (!tackleEnemy.IsTargetWithinHomeChaseDistanceForState())
        {
            tackleEnemy._animator.SetTrigger("Return");
            ChangeTo<TackleEnemyReturn>(null);
            return;
        }

        if (!tackleEnemy.HasTargetForState() || tackleEnemy.Awareness == EnemyAwareness.Unaware)
        {
            tackleEnemy._animator.SetTrigger("Return");
            ChangeTo<TackleEnemyPatrol>(_patrolState);
            return;
        }

        if (tackleEnemy.CanStartCharge())
        {
            tackleEnemy._animator.SetTrigger("Charge");
            ChangeTo<TackleEnemyCharge>(_chargeState);
            return;
        }

        tackleEnemy.SetTargetDestination();
    }
}

public class TackleEnemyCharge : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _tackleState;

    private float _windupTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        tackleEnemy.LockTackleAim();
        _windupTimer = 0.0f;
    }

    public override void Update()
    {
        _windupTimer += Time.deltaTime;

        if (_windupTimer < tackleEnemy.WindupTime)
        {
            return;
        }

        if (tackleEnemy._animator != null)
        {
            tackleEnemy._animator.SetTrigger("Tackle");
        }
        ChangeTo<TackleEnemyTackle>(_tackleState);
    }
}

public class TackleEnemyTackle : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _cooldownState;

    private Vector3 _tackleDirection;
    private float _tackleMoveDistance;
    private float _travelLimit;
    private float _elapsed;
    private bool _hasHitTarget;

    public override void Enter()
    {
        tackleEnemy.SetCommittedAttack(true);
        tackleEnemy.StopMoveForState();
        tackleEnemy.BeginDirectMovementForState();
        tackleEnemy.IgnorePlayerBodyCollision();
        _tackleMoveDistance = 0.0f;
        _elapsed = 0.0f;
        _hasHitTarget = false;
        _travelLimit = tackleEnemy.GetTackleTravelDistance();
        _tackleDirection = tackleEnemy.GetLockedTackleDirection();

        if (_tackleDirection.sqrMagnitude > 0.0001f)
        {
            tackleEnemy.transform.rotation = Quaternion.LookRotation(_tackleDirection);
        }
    }

    public override void Exit()
    {
        tackleEnemy.SetCommittedAttack(false);
        tackleEnemy.EndDirectMovementForState();
        tackleEnemy.ClearLockedTackleDirection();
        tackleEnemy.ReleasePlayerBodyCollision();
    }

    public override void Update()
    {
        if (_tackleDirection.sqrMagnitude > 0.0001f)
        {
            tackleEnemy.transform.rotation = Quaternion.LookRotation(_tackleDirection);
        }

        _elapsed += Time.deltaTime;
        float remaining = _travelLimit - _tackleMoveDistance;

        if (_elapsed >= tackleEnemy.TackleDuration || remaining <= 0.001f)
        {
            ChangeTo<TackleEnemyCooldown>(_cooldownState);
            return;
        }

        float moveDistance = Mathf.Min(tackleEnemy.TackleSpeed * Time.deltaTime, remaining);

        if (tackleEnemy.IsTackleBlocked(_tackleDirection, moveDistance))
        {
            ChangeTo<TackleEnemyCooldown>(_cooldownState);
            return;
        }

        tackleEnemy.MoveDirectForState(_tackleDirection * moveDistance);
        _tackleMoveDistance += moveDistance;

        if (!_hasHitTarget && tackleEnemy.TryTacklePlayer(_tackleDirection))
        {
            _hasHitTarget = true;
        }
    }
}

public class TackleEnemyCooldown : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _moveAreaReturnState;

    private float _waitTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        _waitTimer = 0.0f;
    }

    public override void Update()
    {
        _waitTimer += Time.deltaTime;

        if (_waitTimer < tackleEnemy.PostTackleWait)
        {
            return;
        }

        if (!tackleEnemy.IsSelfInsideMoveArea())
        {
            if (tackleEnemy._animator != null)
            {
                tackleEnemy._animator.SetTrigger("Return");
            }
            ChangeTo<TackleEnemyMoveAreaReturn>(_moveAreaReturnState);
            return;
        }

        if (tackleEnemy.IsTargetStillVisibleForTackle())
        {
            ChangeTo<TackleEnemyAlert>(null);
            return;
        }

        ChangeTo<TackleEnemyPatrol>(_patrolState);
    }
}

public class TackleEnemyReturn : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _chaseState;
    [SerializeField] private EnemyState _chargeState;

    private float _returnTimer;
    private float _homeWaitTimer;
    private int _returnRetryCount;
    private bool _isWaitingAtHome;

    public override void Enter()
    {
        tackleEnemy.OnReturnStateEntered();
        tackleEnemy.EndDirectMovementForState();
        tackleEnemy.ResumeMoveForState();
        tackleEnemy.SetHomeDestinationForState();
        _returnTimer = 0.0f;
        _homeWaitTimer = 0.0f;
        _returnRetryCount = 0;
        _isWaitingAtHome = false;
    }

    public override void Update()
    {
        if (tackleEnemy.Lifecycle == EnemyLifecycle.WaitDespawn
            || tackleEnemy.Lifecycle == EnemyLifecycle.Inactive
            || tackleEnemy.Lifecycle == EnemyLifecycle.Dead)
        {
            tackleEnemy.StopMoveForState();
            return;
        }

        if (CanResumeCombat())
        {
            if (tackleEnemy.CanStartCharge())
            {
                ChangeTo<TackleEnemyCharge>(_chargeState);
            }
            else
            {
                ChangeTo<TackleEnemyChase>(_chaseState);
            }

            return;
        }

        if (_isWaitingAtHome)
        {
            UpdateHomeWait();
            return;
        }

        _returnTimer += Time.deltaTime;

        if (tackleEnemy.HasLeash && tackleEnemy.IsWithinReturnCompleteDistance())
        {
            tackleEnemy.CompleteLeashReturn();
            return;
        }

        if (tackleEnemy.IsAtHomeForState() || tackleEnemy.IsArrivedForState())
        {
            if (tackleEnemy.HasLeash)
            {
                tackleEnemy.CompleteLeashReturn();
                return;
            }

            BeginHomeWait();
            return;
        }

        if (_returnTimer >= tackleEnemy.ReturnImpossibleTime)
        {
            if (_returnRetryCount == 0 && tackleEnemy.TryCorrectToNearbyNavMeshPositionForState())
            {
                tackleEnemy.SetHomeDestinationForState();
                _returnTimer = 0.0f;
                _returnRetryCount++;
                return;
            }

            if (tackleEnemy.HasLeash)
            {
                tackleEnemy.SetHomeDestinationForState();
                return;
            }

            tackleEnemy.Despawn();
            return;
        }

        tackleEnemy.SetHomeDestinationForState();
    }

    private bool CanResumeCombat()
    {
        if (tackleEnemy.HasLeash)
        {
            if (!tackleEnemy.CanResumeFromLeashReturn())
            {
                return false;
            }

            tackleEnemy.MarkCombatResumed();
            return true;
        }

        return tackleEnemy.HasTargetForState()
            && tackleEnemy.IsTargetWithinHomeChaseDistanceForState()
            && tackleEnemy.Awareness == EnemyAwareness.Engaged;
    }

    private void BeginHomeWait()
    {
        _isWaitingAtHome = true;
        _homeWaitTimer = 0.0f;
        tackleEnemy.StopMoveForState();
    }

    private void UpdateHomeWait()
    {
        if (tackleEnemy.IsTargetInSpawnAreaForState())
        {
            ChangeTo<TackleEnemyPatrol>(_patrolState);
            return;
        }

        _homeWaitTimer += Time.deltaTime;

        if (_homeWaitTimer >= tackleEnemy.HomeDespawnWaitTime)
        {
            tackleEnemy.Despawn();
        }
    }
}

public class TackleEnemyMoveAreaReturn : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;

    public override void Enter()
    {
        tackleEnemy.EndDirectMovementForState();
        tackleEnemy.ClearAwarenessForState();
        tackleEnemy.ApplyPatrolSpeed();
        tackleEnemy.SetAgentRotationUpdates(true);
        tackleEnemy.ResumeMoveForState();
        tackleEnemy.SetHomeDestinationForState();
    }

    public override void Update()
    {
        if (tackleEnemy.IsSelfInsideMoveArea())
        {
            tackleEnemy.StopMoveForState();
            tackleEnemy.ClearAwarenessForState();
            ChangeTo<TackleEnemyPatrol>(_patrolState);
            return;
        }

        tackleEnemy.ApplyPatrolSpeed();
        tackleEnemy.ResumeMoveForState();
        tackleEnemy.SetHomeDestinationForState();
    }
}

public class TackleEnemyDown : TackleEnemyState
{
    private float _downTimer;

    public override void Enter()
    {
        tackleEnemy.EndDirectMovementForState();
        tackleEnemy.ReleasePlayerBodyCollision();
        tackleEnemy.StopMoveForState();
        _downTimer = 0.0f;
    }

    public override void Update()
    {
        tackleEnemy.StopMoveForState();
        _downTimer += Time.deltaTime;

        if (_downTimer >= tackleEnemy.DownRecoverTime)
        {
            tackleEnemy.RecoverFromDownForState();
        }
    }
}
