using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RangedEnemy : Enemy
{
    [Header("Ranged Ranges")]
    [SerializeField]
    private float _detectionRange = 15.0f;
    [SerializeField]
    private float _attackRange = 10.0f;

    [Header("Ranged Combat")]
    [SerializeField]
    private float _attackInterval = 2.0f;
    [SerializeField]
    private float _moveSpeed = 3.5f;
    [SerializeField]
    private float _rotationSpeed = 10.0f;
    [SerializeField]
    private Transform _firePoint = null;
    [SerializeField]
    private EnemyBullet _bulletPrefab = null;

    [Header("ダウン")]
    [SerializeField]
    private EnemyState _downState;
    [SerializeField]
    private EnemyState _idleStateAfterDown;
    [SerializeField]
    private EnemyState _returnState;
    [SerializeField]
    private float _downRecoverTime = 30.0f;

    public float DetectionRange => _detectionRange;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float RotationSpeed => _rotationSpeed;
    public float DownRecoverTime => _downRecoverTime;

    protected override void Start()
    {
        ApplyRangedRuntimeSettings();
        base.Start();

        if (stateMachine.GetState<EnemyState>() == null)
        {
            ChangeEnemyState(ScriptableObject.CreateInstance<RangedEnemyIdle>());
        }
    }

    protected override void InitializeEnemyReferences()
    {
        base.InitializeEnemyReferences();

        if (_firePoint == null)
        {
            _firePoint = transform;
        }

        ApplyRangedRuntimeSettings();
    }

    private void OnValidate()
    {
        if (_attackRange > _detectionRange)
        {
            _attackRange = _detectionRange;
        }

        if (_lostDistance < _detectionRange)
        {
            _lostDistance = _detectionRange;
        }

        ApplyRangedRuntimeSettings();
    }

    private void ApplyRangedRuntimeSettings()
    {
        _alertDistance = _detectionRange;
        _attackCooldown = Mathf.Max(0.0f, _attackInterval);

        if (_agent == null)
        {
            TryGetComponent(out _agent);
        }

        if (_agent != null)
        {
            _agent.speed = Mathf.Max(0.0f, _moveSpeed);
        }
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
            : ScriptableObject.CreateInstance<RangedEnemyDown>();

        ChangeEnemyState(nextState);
    }

    protected override void RecoverFromDown()
    {
        base.RecoverFromDown();

        EnemyState nextState = _idleStateAfterDown != null
            ? Instantiate(_idleStateAfterDown)
            : ScriptableObject.CreateInstance<RangedEnemyIdle>();

        ChangeEnemyState(nextState);
    }

    protected override void ReturnToSpawn()
    {
        base.ReturnToSpawn();

        EnemyState nextState = _idleStateAfterDown != null
            ? Instantiate(_idleStateAfterDown)
            : ScriptableObject.CreateInstance<RangedEnemyIdle>();

        ChangeEnemyState(nextState);
    }

    protected override void RestartCombatState()
    {
        ChangeEnemyState(ScriptableObject.CreateInstance<RangedEnemyIdle>());
    }

    public override void BeginReturnToHome()
    {
        EnemyState nextState = _returnState != null
            ? Instantiate(_returnState)
            : ScriptableObject.CreateInstance<RangedEnemyReturn>();

        ChangeEnemyState(nextState);
    }

    public void RecoverFromDownForState()
    {
        RecoverFromDown();
    }

    public bool IsTargetInDetectionRange()
    {
        if (!IsTargetWithinHomeChaseDistance())
        {
            return false;
        }

        if (!IsTargetInMoveArea())
        {
            return IsTargetInAttackRange();
        }

        return IsTargetInDistance(_detectionRange) || IsTargetInAttackRange();
    }

    public bool IsTargetInAttackRange()
    {
        return IsTargetWithinHomeChaseDistance() && IsTargetInDistance(_attackRange);
    }

    public bool IsTargetLostForRanged()
    {
        if (!IsTargetWithinHomeChaseDistance())
        {
            return true;
        }

        if (!IsTargetInMoveArea())
        {
            return !IsTargetInAttackRange();
        }

        return IsTargetLost();
    }

    public void SetTargetDestination()
    {
        if (_target == null)
        {
            return;
        }

        SetMoveDestination(_target.position);
    }

    public void LookAtTargetForCombat()
    {
        LookAtTarget(_rotationSpeed);
    }

    public bool TryFireBullet()
    {
        if (IsDead || IsDespawning || IsDown || _bulletPrefab == null || _target == null)
        {
            return false;
        }

        if (!HasLineOfSightToTarget())
        {
            return false;
        }

        Transform firePoint = _firePoint != null ? _firePoint : transform;
        Vector3 aimPoint = GetTargetAimPoint();
        Vector3 launchDirection = aimPoint - firePoint.position;

        if (launchDirection.sqrMagnitude <= 0.0001f)
        {
            launchDirection = firePoint.forward;
        }

        EnemyBullet bullet = Instantiate(
            _bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(launchDirection.normalized));

        if (!bullet.LaunchAt(aimPoint, this))
        {
            Destroy(bullet.gameObject);
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
