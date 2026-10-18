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
    private float _attackInterval = 4.5f;
    [SerializeField]
    private float _moveSpeed = 3.5f;
    [SerializeField]
    private float _rotationSpeed = 90.0f;
    [SerializeField]
    private Transform _firePoint = null;
    [SerializeField]
    private EnemyBullet _bulletPrefab = null;

    [Header("Mortar Preparation")]
    [SerializeField] private Transform _turretPivot;
    [SerializeField, Min(0.5f)] private float _preparationTime = 1.5f;
    [SerializeField, Min(0)] private float _aimLockTime = 0.5f;
    [SerializeField, Min(0.01f)] private float _flightTime = 3f;
    [SerializeField, Min(0.01f)] private float _blastRadius = 2f;
    [SerializeField, Min(0)] private float _rotationAngleMultiplier = 2f;
    [SerializeField, Min(1)] private float _maximumRotationSpeed = 360f;
    [SerializeField] private LayerMask _landingSurfaceMask = ~0;

    private readonly MortarAim _aim = new MortarAim();
    private float _preparationElapsed;
    private float _nextAttackTime;
    private bool _preparing;
    private bool _aimAligned;

    public override bool IsStationary => true;
    protected override bool UsesWarpMoveAreaReturn => false;
    public bool IsPreparing => _preparing;
    public bool IsAimLocked => _preparing && _aim.IsLocked;
    public bool CanBeginPreparation => Time.time >= _nextAttackTime;
    public Vector3 LockedImpactPoint => _aim.Point;

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
        ApplyRangedRuntimeSettings();

        if (TryGetComponent<Rigidbody>(out var body))
        {
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.useGravity = false;
            // Rotating upper-body colliders must not recalculate the base's center of mass.
            body.centerOfMass = Vector3.zero;
        }

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
        _preparationTime = Mathf.Max(0.5f, _preparationTime);
        _aimLockTime = Mathf.Clamp(_aimLockTime, 0, _preparationTime);
        _flightTime = Mathf.Max(0.01f, _flightTime);
        _blastRadius = Mathf.Max(0.01f, _blastRadius);
        if (_attackRange > _detectionRange)
        {
            _attackRange = _detectionRange;
        }

        if (_lostDistance < _detectionRange)
        {
            _lostDistance = _detectionRange;
        }

        ApplyRangedRuntimeSettings();
        ValidateSealPrefab();
    }

    private void ApplyRangedRuntimeSettings()
    {
        _alertDistance = _detectionRange;
        _attackCooldown = Mathf.Max(0.0f, _attackInterval);

        _moveSpeed = Mathf.Max(0.0f, _moveSpeed);

        if (_agent == null)
        {
            TryGetComponent(out _agent);
        }

        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            StopMove();
        }
        if (_agent != null && Application.isPlaying)
        {
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.enabled = false;
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
        StopMove();
    }

    public void LookAtTargetForCombat()
    {
        if (_target != null && !_preparing && !IsCombatAIFrozen)
            RotateTurret(GetLandingPoint(), Time.deltaTime);
    }

    private Vector3 GetLandingPoint()
    {
        Vector3 point = _target.position;
        RaycastHit[] hits = Physics.RaycastAll(GetTargetAimPoint() + Vector3.up * 0.1f,
            Vector3.down, 100f, _landingSurfaceMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == _target || hit.transform.IsChildOf(_target)) continue;
            if (hit.collider.GetComponentInParent<Character>() != null) continue;
            return hit.point;
        }
        return point;
    }

    public bool BeginPreparation()
    {
        if (!CanBeginPreparation || _target == null || _bulletPrefab == null || _turretPivot == null
            || IsDead || IsDown || IsDespawning || IsCombatAIFrozen || BlocksCombatTransition)
            return false;
        _aim.Begin(GetLandingPoint(), _blastRadius, _aimLockTime);
        _preparationElapsed = 0;
        _preparing = true;
        _aimAligned = false;
        return true;
    }

    public void CancelPreparation()
    {
        _preparing = false;
        _aimAligned = false;
        SetCommittedAttack(false);
    }

    public void TickPreparation(float deltaTime)
    {
        if (!_preparing) return;
        if (IsDead || IsDown || IsDespawning || IsCombatAIFrozen || BlocksCombatTransition)
        {
            CancelPreparation();
            return;
        }
        _preparationElapsed += Mathf.Max(0, deltaTime);
        if (!_aim.IsLocked)
        {
            if (_target == null || !HasLineOfSightToTarget())
            {
                CancelPreparation();
                return;
            }
            _aim.Sample(GetLandingPoint(), _preparationElapsed);
            if (_aim.IsLocked) SetCommittedAttack(true);
        }
        _aimAligned = RotateTurret(_aim.Point, deltaTime);
        if (_preparationElapsed < _preparationTime) return;
        TryFireBullet();
        CancelPreparation();
    }

    private bool RotateTurret(Vector3 point, float deltaTime)
    {
        if (_turretPivot == null || _bulletPrefab == null) return false;
        Transform muzzle = _firePoint != null ? _firePoint : _turretPivot;
        Vector3 velocity = _bulletPrefab.GetTimedLaunchVelocity(muzzle.position, point, _flightTime);
        if (velocity.sqrMagnitude < 0.0001f) return false;
        // Compensate for a muzzle that has its own local rotation under the upper body.
        Quaternion desired = Quaternion.LookRotation(velocity) * Quaternion.Inverse(muzzle.rotation) * _turretPivot.rotation;
        float angle = Quaternion.Angle(_turretPivot.rotation, desired);
        float speed = Mathf.Min(_maximumRotationSpeed, Mathf.Max(0, _rotationSpeed) + angle * _rotationAngleMultiplier);
        _turretPivot.rotation = Quaternion.RotateTowards(_turretPivot.rotation, desired, speed * Mathf.Max(0, deltaTime));
        // Recompute after rotating: the muzzle may be offset from the pivot.
        velocity = _bulletPrefab.GetTimedLaunchVelocity(muzzle.position, point, _flightTime);
        return Vector3.Angle(muzzle.forward, velocity) <= 0.1f;
    }

    public bool TryFireBullet()
    {
        if (IsDead || IsDespawning || IsDown || IsCombatAIFrozen || BlocksCombatTransition
            || _bulletPrefab == null || !_preparing || !_aim.IsLocked || !_aimAligned
            || _preparationElapsed < _preparationTime || !CanBeginPreparation)
        {
            return false;
        }

        Transform firePoint = _firePoint != null ? _firePoint : transform;
        Vector3 aimPoint = _aim.Point;
        Vector3 launchDirection = aimPoint - firePoint.position;

        if (launchDirection.sqrMagnitude <= 0.0001f)
        {
            launchDirection = firePoint.forward;
        }

        EnemyBullet bullet = Instantiate(
            _bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(launchDirection.normalized));

        if (!bullet.LaunchTimedArea(aimPoint, this, _flightTime, _blastRadius, _attackDamage))
        {
            Destroy(bullet.gameObject);
            return false;
        }

        _nextAttackTime = Time.time + Mathf.Max(0, _attackInterval);
        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
        if (_preparing)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_aim.AreaCenter, _blastRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_aim.Point, _blastRadius);
        }
    }
}

/// <summary>One preparation's fixed search area and last valid target position.</summary>
public sealed class MortarAim
{
    public Vector3 Point { get; private set; }
    public Vector3 AreaCenter { get; private set; }
    public bool IsLocked { get; private set; }
    private float _radius;
    private float _lockTime;

    public void Begin(Vector3 point, float radius, float lockTime)
    {
        Point = AreaCenter = point;
        _radius = Mathf.Max(0, radius);
        _lockTime = Mathf.Max(0, lockTime);
        IsLocked = false;
    }

    public void Sample(Vector3 point, float elapsed)
    {
        if (IsLocked) return;
        Vector3 offset = point - AreaCenter;
        offset.y = 0;
        if (offset.sqrMagnitude > _radius * _radius)
        {
            IsLocked = true;
            return;
        }

        Point = point;
        IsLocked = elapsed >= _lockTime;
    }
}
