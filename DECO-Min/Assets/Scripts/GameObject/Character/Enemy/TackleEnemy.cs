using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 一定範囲を徘徊し、プレイヤーを発見するとチャージタックル攻撃を行う敵AI
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TackleEnemy : Enemy
{
    [Header("徘徊")]
    [SerializeField]
    private Transform _patrolCenter = null;
    [SerializeField]
    private float _patrolRadius = 8.0f;
    [SerializeField]
    private float _patrolTurnWait = 2.0f;
    [SerializeField]
    private float _patrolTurnDuration = 0.4f;

    [Header("警戒")]
    [SerializeField]
    private float _alertTime = 1.5f;
    [SerializeField]
    private float _aimFollowTime = 1.0f;

    [Header("タックル攻撃")]
    [SerializeField]
    private float _baseMoveSpeed = 3.0f;
    [SerializeField]
    private float _attackStartDistance = 3.0f;
    [SerializeField]
    private float _windupTime = 2.0f;
    [SerializeField]
    private float _tackleDuration = 2.0f;
    [SerializeField]
    private float _tackleOvershootTime = 0.4f;
    [SerializeField]
    private float _postTackleWait = 1.0f;
    [SerializeField]
    private float _tackleHitRadius = 0.7f;
    [SerializeField]
    private float _knockbackDistance = 1.5f;
    [SerializeField]
    private float _knockbackDuration = 0.2f;
    [SerializeField]
    private LayerMask _obstacleLayer;

    [Header("ダウン")]
    [SerializeField]
    private EnemyState _downState;
    [SerializeField]
    private EnemyState _patrolStateAfterDown;
    [SerializeField]
    private EnemyState _returnState;
    [SerializeField]
    private float _downRecoverTime = 30.0f;

    private Vector3 _lockedTackleDirection;
    private bool _hasLockedTackleDirection;
    private Vector3 _aimPoint;
    private bool _hasAimPoint;

    public float AlertTime => _alertTime;
    public float AimFollowTime => Mathf.Max(0.0f, _aimFollowTime);
    public float WindupTime => Mathf.Max(0.0f, _windupTime);
    public float TackleDuration => Mathf.Max(0.0f, _tackleDuration);
    public float PostTackleWait => Mathf.Max(0.0f, _postTackleWait);
    public float PatrolTurnWait => Mathf.Max(0.0f, _patrolTurnWait);
    public float PatrolTurnDuration => Mathf.Max(0.01f, _patrolTurnDuration);
    public float TackleSpeed => Mathf.Max(0.1f, _baseMoveSpeed) * 2.0f;
    public float PatrolMoveSpeed => Mathf.Max(0.1f, _baseMoveSpeed) * 0.5f;
    public float DownRecoverTime => _downRecoverTime;
    public Vector3 AimPoint => _aimPoint;
    public bool HasAimPoint => _hasAimPoint;

    private void Awake()
    {
        ConfigureBodyForAgentMovement();
    }

    protected override void Start()
    {
        ConfigureBodyForAgentMovement();
        base.Start();

        if (stateMachine.GetState<EnemyState>() == null)
        {
            ChangeEnemyState(ScriptableObject.CreateInstance<TackleEnemyPatrol>());
        }
    }

    protected override float GetAlertConfirmTime()
    {
        return _alertTime;
    }

    protected override bool UsesWarpMoveAreaReturn => false;

    protected override float GetForcedPerceptionRange()
    {
        return 0.0f;
    }

    private void ConfigureBodyForAgentMovement()
    {
        if (TryGetComponent(out Rigidbody body))
        {
            body.useGravity = false;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (!TryGetComponent(out BoxCollider box) || !TryGetComponent(out NavMeshAgent agent))
        {
            return;
        }

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        scaleY = scaleY < 0.001f ? 1.0f : scaleY;
        float bottom = scaleY * (box.center.y - box.size.y * 0.5f);

        if (bottom >= -agent.baseOffset + 0.01f)
        {
            return;
        }

        Vector3 center = box.center;
        center.y = box.size.y * 0.5f - agent.baseOffset / scaleY;
        box.center = center;
    }

    protected override void EnterDown()
    {
        base.EnterDown();

        if (BlocksCombatTransition)
        {
            return;
        }

        EnemyState nextState = _downState != null
            ? Instantiate(_downState)
            : ScriptableObject.CreateInstance<TackleEnemyDown>();

        ChangeEnemyState(nextState);
    }

    protected override void RecoverFromDown()
    {
        base.RecoverFromDown();

        EnemyState nextState = _patrolStateAfterDown != null
            ? Instantiate(_patrolStateAfterDown)
            : ScriptableObject.CreateInstance<TackleEnemyPatrol>();

        ChangeEnemyState(nextState);
    }

    public bool HasTargetForState()
    {
        return HasTarget();
    }

    public bool IsTargetInAlertDistanceForState()
    {
        if (!IsTargetWithinHomeChaseDistanceForState())
        {
            return false;
        }

        if (!IsTargetInMoveArea())
        {
            return IsTargetInAttackStartDistance();
        }

        return IsTargetInAlertDistance() || IsTargetInAttackStartDistance();
    }

    public bool IsTargetLostForState()
    {
        if (!IsTargetWithinHomeChaseDistanceForState())
        {
            return true;
        }

        if (!IsTargetInMoveArea())
        {
            return !IsTargetInAttackStartDistance();
        }

        return IsTargetLost();
    }

    public bool IsTargetInAttackStartDistance()
    {
        return IsTargetWithinHomeChaseDistanceForState() && IsTargetInDistance(_attackStartDistance);
    }

    public bool CanStartCharge()
    {
        return IsTargetInAttackStartDistance() && HasLineOfSightToTarget();
    }

    public bool IsTargetWithinHomeChaseDistanceForState()
    {
        return IsTargetWithinHomeChaseDistance();
    }

    public bool IsTargetInSpawnAreaForState()
    {
        return IsTargetInSpawnArea();
    }

    public Vector3 GetDirectionToTargetForState()
    {
        return GetDirectionToTarget();
    }

    public void LookAtTargetForState()
    {
        LookAtTarget();
    }

    public bool IsTargetVisibleForTackle()
    {
        if (!HasTarget() || !HasLineOfSightToTarget() || !IsTargetWithinHomeChaseDistanceForState())
        {
            return false;
        }

        if (HasMoveArea && IsTargetInMoveArea())
        {
            return true;
        }

        return IsTargetInFieldOfView() && IsTargetInDistance(_alertDistance);
    }

    public bool IsTargetStillVisibleForTackle()
    {
        if (!HasTarget() || !HasLineOfSightToTarget() || IsTargetLost())
        {
            return false;
        }

        if (HasMoveArea && IsTargetInMoveArea())
        {
            return true;
        }

        return IsTargetInFieldOfView();
    }

    public void RememberVisibleAimPoint()
    {
        if (_target == null || !HasLineOfSightToTarget())
        {
            return;
        }

        Vector3 point = _target.position;
        point.y = transform.position.y;
        _aimPoint = point;
        _hasAimPoint = true;
    }

    public void LookAtHorizontalPoint(Vector3 point, float rotateSpeed = 10.0f)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0.0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotateSpeed);
    }

    public void LockTackleAim()
    {
        Vector3 direction = _hasAimPoint ? _aimPoint - transform.position : transform.forward;
        direction.y = 0.0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = GetDirectionToTarget();
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        _lockedTackleDirection = direction.normalized;
        _hasLockedTackleDirection = true;
        transform.rotation = Quaternion.LookRotation(_lockedTackleDirection);
    }

    public void ClearLockedTackleDirection()
    {
        _hasLockedTackleDirection = false;
        _lockedTackleDirection = Vector3.zero;
        _hasAimPoint = false;
        _aimPoint = Vector3.zero;
    }

    public float GetTackleTravelDistance()
    {
        float speed = TackleSpeed;
        float maxDistance = speed * TackleDuration;
        float overshoot = speed * Mathf.Max(0.0f, _tackleOvershootTime);
        Vector3 point = _hasAimPoint ? _aimPoint : transform.position;
        Vector3 delta = point - transform.position;
        delta.y = 0.0f;
        return Mathf.Min(maxDistance, delta.magnitude + overshoot);
    }

    public Vector3 GetLockedTackleDirection()
    {
        if (_hasLockedTackleDirection && _lockedTackleDirection.sqrMagnitude > 0.0001f)
        {
            return _lockedTackleDirection;
        }

        Vector3 direction = GetDirectionToTarget();

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
            direction.y = 0.0f;
        }

        return direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
    }

    public void SetTargetDestination()
    {
        if (_target == null)
        {
            return;
        }

        SetMoveDestination(_target.position);
    }

    public void StopMoveForState()
    {
        StopMove();
    }

    public void ResumeMoveForState()
    {
        ResumeMove();
    }

    public void MoveDirectForState(Vector3 moveValue)
    {
        MoveDirect(moveValue);
    }

    public void BeginDirectMovementForState()
    {
        BeginDirectMovement();
    }

    public void EndDirectMovementForState()
    {
        EndDirectMovement();
    }

    public bool IsArrivedForState()
    {
        return IsArrived();
    }

    public bool IsAtHomeForState()
    {
        return IsAtHome();
    }

    public void SetHomeDestinationForState()
    {
        SetHomeDestination();
    }

    public bool TryCorrectToNearbyNavMeshPositionForState()
    {
        return TryCorrectToNearbyNavMeshPosition();
    }

    public void ResetAttackCooldownForState()
    {
        ResetAttackCooldown();
    }

    public bool UpdateAttackCooldownForState()
    {
        return UpdateAttackCooldown();
    }

    public void RecoverFromDownForState()
    {
        RecoverFromDown();
    }

    public bool IsTackleBlocked(Vector3 direction, float distance)
    {
        Vector3 flatDirection = direction;
        flatDirection.y = 0.0f;

        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        flatDirection.Normalize();
        const float skin = 0.2f;
        Vector3 origin = transform.position + Vector3.up * 0.4f - flatDirection * skin;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            flatDirection,
            Mathf.Max(0.05f, distance) + skin,
            ObstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null || hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public void ResetToHomeForState()
    {
        ReturnToSpawn();
        ClearAwarenessForState();
    }

    public void ApplyPatrolSpeed()
    {
        if (_agent != null)
        {
            _agent.speed = PatrolMoveSpeed;
        }
    }

    public void SetAgentRotationUpdates(bool updateRotation)
    {
        if (_agent != null)
        {
            _agent.updateRotation = updateRotation;
        }
    }

    public bool TryChoosePatrolDestination(out Vector3 destination)
    {
        Vector3 origin = transform.position;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            if (!TryPickPatrolCandidate(out Vector3 candidate))
            {
                break;
            }

            Vector3 flat = candidate - origin;
            flat.y = 0.0f;

            if (flat.sqrMagnitude >= 1.0f)
            {
                destination = candidate;
                return true;
            }
        }

        return TryPickPatrolCandidate(out destination);
    }

    public bool HasReachedPatrolPoint(Vector3 point)
    {
        Vector3 delta = transform.position - point;
        delta.y = 0.0f;

        if (delta.sqrMagnitude <= 0.36f)
        {
            return true;
        }

        if (_agent == null || !_agent.enabled || _agent.pathPending || !_agent.hasPath)
        {
            return false;
        }

        return _agent.remainingDistance <= Mathf.Max(0.2f, _agent.stoppingDistance + 0.2f);
    }

    public void BeginPatrolMove(Vector3 destination)
    {
        ApplyPatrolSpeed();
        SetAgentRotationUpdates(false);
        ResumeMove();
        SetMoveDestination(destination);
    }

    public bool TryTacklePlayer(Vector3 tackleDirection)
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Collider[] hitColliders = Physics.OverlapSphere(origin, _tackleHitRadius, _targetLayer);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider hitCollider = hitColliders[i];

            if (hitCollider == null)
            {
                continue;
            }

            Player player = hitCollider.GetComponent<Player>();

            if (player == null)
            {
                player = hitCollider.GetComponentInParent<Player>();
            }

            if (player == null || !player.TryDamage(_attackDamage))
            {
                continue;
            }

            player.ApplyKnockback(tackleDirection, _knockbackDistance, _knockbackDuration);
            return true;
        }

        return false;
    }

    private bool TryPickPatrolCandidate(out Vector3 destination)
    {
        if (TryGetRandomMoveAreaPoint(out destination))
        {
            return true;
        }

        if (HasMoveArea)
        {
            destination = transform.position;
            return false;
        }

        Vector3 origin = _patrolCenter != null ? _patrolCenter.position : HomePosition;
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _patrolRadius;
        Vector3 randomPoint = origin + new Vector3(randomCircle.x, 0.0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            destination = hit.position;
            return true;
        }

        destination = transform.position;
        return false;
    }

    private LayerMask ObstacleMask
    {
        get
        {
            if (_obstacleLayer.value != 0)
            {
                return _obstacleLayer;
            }

            return LayerMask.GetMask("Ground");
        }
    }

    protected override void ReturnToSpawn()
    {
        base.ReturnToSpawn();

        EnemyState nextState = _patrolStateAfterDown != null
            ? Instantiate(_patrolStateAfterDown)
            : ScriptableObject.CreateInstance<TackleEnemyPatrol>();

        ChangeEnemyState(nextState);
    }

    protected override void RestartCombatState()
    {
        ChangeEnemyState(ScriptableObject.CreateInstance<TackleEnemyPatrol>());
    }

    public override void BeginReturnToHome()
    {
        EnemyState nextState = _returnState != null
            ? Instantiate(_returnState)
            : ScriptableObject.CreateInstance<TackleEnemyReturn>();

        ChangeEnemyState(nextState);
    }

    protected override void OnDrawGizmosSelected()
    {
        DrawVisionFanGizmo(new Color(0.45f, 0.26f, 0.13f));
        DrawMoveAreaGizmo(Color.yellow);
    }
}
