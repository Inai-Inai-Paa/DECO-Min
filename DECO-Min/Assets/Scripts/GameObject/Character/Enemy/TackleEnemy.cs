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
    private float _patrolPointInterval = 3.0f;

    [Header("警戒")]
    [SerializeField]
    private float _alertTime = 1.5f;

    [Header("タックル攻撃")]
    [SerializeField]
    private float _attackStartDistance = 3.0f;
    [SerializeField]
    private float _chargeTime = 1.0f;
    [SerializeField]
    private float _tackleSpeed = 12.0f;
    [SerializeField]
    private float _tackleDistance = 6.0f;
    [SerializeField]
    private float _tackleHitRadius = 0.7f;
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

    public float PatrolPointInterval => _patrolPointInterval;
    public float AlertTime => _alertTime;
    public float ChargeTime => _chargeTime;
    public float TackleSpeed => _tackleSpeed;
    public float TackleDistance => _tackleDistance;
    public float DownRecoverTime => _downRecoverTime;

    protected override void Start()
    {
        base.Start();

        if (stateMachine.GetState<EnemyState>() == null)
        {
            ChangeEnemyState(ScriptableObject.CreateInstance<TackleEnemyPatrol>());
        }
    }

    protected override void InitializeEnemyReferences()
    {
        base.InitializeEnemyReferences();

        if (_patrolCenter == null)
        {
            _patrolCenter = transform;
        }
    }

    protected override void EnterDown()
    {
        base.EnterDown();

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
        return Physics.Raycast(transform.position, direction, distance, _obstacleLayer);
    }

    public void SetRandomPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * _patrolRadius;
        Vector3 randomPoint = _patrolCenter.position + new Vector3(randomCircle.x, 0.0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            SetMoveDestination(hit.position);
        }
    }

    public bool TryTackleHit()
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            _tackleHitRadius,
            _targetLayer
        );

        foreach (Collider hitCollider in hitColliders)
        {
            if (TryAttackDamage(hitCollider))
            {
                return true;
            }
        }

        return false;
    }

    protected override void ReturnToSpawn()
    {
        base.ReturnToSpawn();

        EnemyState nextState = _patrolStateAfterDown != null
            ? Instantiate(_patrolStateAfterDown)
            : ScriptableObject.CreateInstance<TackleEnemyPatrol>();

        ChangeEnemyState(nextState);
    }

    public override void BeginReturnToHome()
    {
        EnemyState nextState = _returnState != null
            ? Instantiate(_returnState)
            : ScriptableObject.CreateInstance<TackleEnemyReturn>();

        ChangeEnemyState(nextState);
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = _patrolCenter != null ? _patrolCenter : transform;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center.position, _patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _alertDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackStartDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _tackleHitRadius);
    }
}
