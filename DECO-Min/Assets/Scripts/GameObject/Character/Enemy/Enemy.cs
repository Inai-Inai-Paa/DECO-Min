using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public enum EnemyDamageModel
{
    Seal,
    Health
}

public enum EnemyRemovalReason
{
    None,
    Defeated,
    Despawned
}

public enum EnemyAwareness
{
    Unaware,
    Alert,
    Engaged
}

public enum EnemyLifecycle
{
    Inactive,
    Spawn,
    Combat,
    Return,
    WaitDespawn,
    Dead
}

/// <summary>
/// 敵キャラクターの共通処理を管理する基底クラス
/// </summary>
public partial class Enemy : Character, IDamageable
{
    [Header("ステート")]
    [Space(2)]
    [SerializeField, FormerlySerializedAs("initState")]
    private EnemyState _initState = null;

    [Header("共通参照")]
    [Space(2)]
    [SerializeField]
    protected Transform _target = null;
    [SerializeField]
    protected NavMeshAgent _agent = null;
    [SerializeField]
    protected LayerMask _targetLayer;

    [Header("接地判定")]
    [Space(2)]
    [SerializeField, FormerlySerializedAs("groundCheckPos")]
    private Vector3 _groundCheckPos = Vector3.zero;
    [SerializeField, FormerlySerializedAs("groundLayer")]
    private LayerMask _groundLayer;
    [SerializeField]
    private float _groundCheckDistance = 0.2f;

    [Header("索敵共通")]
    [Space(2)]
    [SerializeField]
    protected float _alertDistance = 8.0f;
    [SerializeField]
    protected float _lostDistance = 12.0f;
    [SerializeField, Range(1.0f, 360.0f)]
    private float _viewAngle = 120.0f;
    [SerializeField, Min(0.0f)]
    private float _eyeHeight = 1.4f;
    [SerializeField]
    private LayerMask _visionBlockMask = ~0;
    [SerializeField, Min(0.0f)]
    private float _alertConfirmTime = 0.8f;
    [SerializeField, Min(0.0f)]
    private float _sightMemoryDuration = 1.5f;

    [Header("Move Area")]
    [Space(2)]
    [SerializeField]
    private bool _useMoveArea = false;
    [SerializeField]
    private Transform _moveAreaCenter = null;
    [SerializeField]
    private Vector3 _moveAreaOffset = Vector3.zero;
    [SerializeField]
    private Vector3 _moveAreaSize = new Vector3(16.0f, 8.0f, 16.0f);
    [SerializeField]
    private float _returnToSpawnDelay = 5.0f;
    [SerializeField]
    private float _navMeshSearchDistance = 3.0f;

    [Header("Home")]
    [SerializeField]
    private float _maxChaseDistanceFromHome = 18.0f;
    [SerializeField]
    private float _returnImpossibleTime = 8.0f;
    [SerializeField]
    private float _homeDespawnWaitTime = 2.0f;

    [Header("攻撃共通")]
    [Space(2)]
    [SerializeField]
    protected int _attackDamage = 10;
    [SerializeField]
    protected float _attackCooldown = 1.5f;

    protected float _cooldownTimer;

    [Header("Damage")]
    [SerializeField]
    private EnemyDamageModel _damageModel = EnemyDamageModel.Seal;

    [Header("Mission")]
    [SerializeField]
    private EnemyData _enemyData;

    [Header("ダウン共通")]
    [Space(2)]
    [SerializeField]
    private int _maxSealHealth = 3;
    [SerializeField]
    private string _sealAttackTag = "PlayerAttack";
    [SerializeField]
    private string _finisherTag = "PlayerFinisher";
    [SerializeField]
    private bool _treatSealAttackAsFinisherWhenDown = true;

    [SerializeField] private GameObject _mySealPrefab;

    private bool _isGrounded;
    private int _currentSealHealth;
    private bool _isDown;
    private bool _isDead;
    private bool _isDespawning;
    private bool _hasNotifiedRemoved;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private float _outsideMoveAreaTimer;
    private bool _isUsingDirectMovement;
    private bool _hasSpawnPose;
    private EnemySpawner _spawnOwner;
    private EnemyRemovalReason _removalReason;
    private EnemyAwareness _awareness = EnemyAwareness.Unaware;
    private float _awarenessTimer;
    private float _lostSightTimer;
    private EnemyLifecycle _lifecycle = EnemyLifecycle.Combat;
    private EnemySpawnArea _spawnArea;
    private bool _isCommittedAttack;
    private float _waitDespawnTimer;
    private float _leashSampleTimer;
    private float _storedAgentSpeed;
    private bool _insideLeash = true;

    public bool IsGrounded => _isGrounded;
    public bool IsDown => _isDown;
    public bool IsDead => _isDead;
    public bool IsDespawning => _isDespawning;
    public Vector3 HomePosition => _spawnPosition;
    public Quaternion HomeRotation => _spawnRotation;
    public float ReturnImpossibleTime => _returnImpossibleTime;
    public float HomeDespawnWaitTime => _homeDespawnWaitTime;
    public EnemyRemovalReason RemovalReason => _removalReason;
    public EnemyAwareness Awareness => _awareness;
    public EnemyLifecycle Lifecycle => _lifecycle;
    public bool HasLeash => _spawnArea != null;
    public bool IsCommittedAttack => _isCommittedAttack;
    public bool BlocksCombatTransition =>
        _lifecycle == EnemyLifecycle.Inactive
        || _lifecycle == EnemyLifecycle.Return
        || _lifecycle == EnemyLifecycle.WaitDespawn
        || _lifecycle == EnemyLifecycle.Dead;
    public event Action<Enemy> Died;
    public event Action<Enemy> Removed;

    protected override void Start()
    {
        InitializeRuntimeStatus();
        base.Start();

        InitializeEnemyReferences();
        SetSpawnPose(transform.position, transform.rotation);
        ResetRuntimeStatus();

        if (_initState != null)
        {
            ChangeEnemyState(Instantiate(_initState));
        }
    }

    protected override void Update()
    {
        TickPerception();
        base.Update();
        UpdateMoveAreaReturn();
        TickLeashSample();
        TickWaitDespawn();
    }

    protected override void FixedUpdate()
    {
        UpdateGroundCheck();

        base.FixedUpdate();
    }

    /// <summary>
    /// 敵が使用する共通参照を初期化する
    /// </summary>
    protected virtual void InitializeEnemyReferences()
    {
        if (_agent == null)
        {
            TryGetComponent(out _agent);
        }

        if (_target == null)
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag("Player");

            if (targetObject != null)
            {
                _target = targetObject.transform;
            }
        }
    }

    /// <summary>
    /// 接地判定を更新する
    /// </summary>
    protected virtual void UpdateGroundCheck()
    {
        _isGrounded = Physics.Raycast(
            transform.position + _groundCheckPos,
            Vector3.down,
            _groundCheckDistance,
            _groundLayer
        );
    }

    /// <summary>
    /// 敵用ステートを変更する
    /// </summary>
    public void ChangeEnemyState(EnemyState nextState)
    {
        if (nextState == null)
        {
            return;
        }

        nextState.Initialize(this, stateMachine);
        stateMachine.ChangeState(nextState);
    }

    /// <summary>
    /// ターゲットが存在するか確認する
    /// </summary>
    public bool HasTarget()
    {
        return _target != null;
    }

    /// <summary>
    /// ターゲットとの距離を取得する
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (_target == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(transform.position, _target.position);
    }

    /// <summary>
    /// ターゲットが指定距離内にいるか確認する
    /// </summary>
    public bool IsTargetInDistance(float distance)
    {
        return GetDistanceToTarget() <= distance;
    }

    /// <summary>
    /// ターゲットが警戒距離内にいるか確認する
    /// </summary>
    public bool IsTargetInAlertDistance()
    {
        return IsTargetInDistance(_alertDistance);
    }

    /// <summary>
    /// ターゲットを見失ったか確認する
    /// </summary>
    public bool IsTargetLost()
    {
        return GetDistanceToTarget() >= _lostDistance;
    }

    public bool IsTargetInFieldOfView()
    {
        if (_target == null)
        {
            return false;
        }

        Vector3 toTarget = GetTargetAimPoint() - GetEyePosition();
        toTarget.y = 0.0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        float halfAngle = _viewAngle * 0.5f;
        return Vector3.Angle(GetFlatForward(), toTarget) <= halfAngle;
    }

    public bool HasLineOfSightToTarget()
    {
        if (_target == null)
        {
            return false;
        }

        Vector3 origin = GetEyePosition();
        Vector3 targetPoint = GetTargetAimPoint();
        Vector3 delta = targetPoint - origin;
        float distance = delta.magnitude;

        if (distance <= 0.05f)
        {
            return true;
        }

        Vector3 direction = delta / distance;
        const float skin = 0.05f;
        RaycastHit[] hits = Physics.RaycastAll(
            origin + direction * skin,
            direction,
            Mathf.Max(0.0f, distance - skin),
            _visionBlockMask,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsOwnCollider(hit.collider))
            {
                continue;
            }

            return IsTargetCollider(hit.collider);
        }

        return true;
    }

    public Vector3 GetTargetAimPoint()
    {
        if (_target == null)
        {
            return transform.position;
        }

        Collider targetCollider = _target.GetComponent<Collider>();

        if (targetCollider == null)
        {
            targetCollider = _target.GetComponentInChildren<Collider>();
        }

        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return _target.position + Vector3.up * 1.0f;
    }

    /// <summary>
    /// ターゲット方向の水平ベクトルを取得する
    /// </summary>
    public Vector3 GetDirectionToTarget()
    {
        if (_target == null)
        {
            return Vector3.zero;
        }

        Vector3 direction = _target.position - transform.position;
        direction.y = 0.0f;

        return direction.normalized;
    }

    /// <summary>
    /// ターゲットの方向を向く
    /// </summary>
    public void LookAtTarget(float rotateSpeed = 10.0f)
    {
        Vector3 direction = GetDirectionToTarget();

        if (direction == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotateSpeed
        );
    }

    /// <summary>
    /// NavMeshAgentの目的地を設定する
    /// </summary>
    public void SetMoveDestination(Vector3 destination)
    {
        if (_agent == null || !_agent.enabled)
        {
            return;
        }

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// 敵の移動を停止する
    /// </summary>
    public void StopMove()
    {
        if (_agent == null || !_agent.enabled)
        {
            return;
        }

        _agent.isStopped = true;
    }

    /// <summary>
    /// 敵の移動を再開する
    /// </summary>
    public void ResumeMove()
    {
        if (_agent == null || !_agent.enabled)
        {
            return;
        }

        _agent.isStopped = false;
    }

    /// <summary>
    /// NavMeshAgentで直線移動する
    /// </summary>
    public void MoveDirect(Vector3 moveValue)
    {
        if (_agent != null && _agent.enabled && !_isUsingDirectMovement)
        {
            _agent.Move(moveValue);
            return;
        }

        transform.position += moveValue;
    }

    /// <summary>
    /// NavMeshAgentが目的地に到着したか確認する
    /// </summary>
    public bool IsArrived()
    {
        if (_agent == null || !_agent.enabled || _agent.pathPending)
        {
            return false;
        }

        return _agent.remainingDistance <= _agent.stoppingDistance;
    }

    public void SetSpawnPose(Vector3 position, Quaternion rotation)
    {
        _spawnPosition = position;
        _spawnRotation = rotation;
        _hasSpawnPose = true;
    }

    public void SetSpawnOwner(EnemySpawner spawnOwner)
    {
        _spawnOwner = spawnOwner;
    }

    public void BindSpawnArea(EnemySpawnArea spawnArea)
    {
        _spawnArea = spawnArea;
    }

    public void SetCommittedAttack(bool isCommitted)
    {
        _isCommittedAttack = isCommitted;
    }

    public void PrepareInactive()
    {
        if (_isDead)
        {
            return;
        }

        _lifecycle = EnemyLifecycle.Inactive;
        _isCommittedAttack = false;
        StopMove();
        gameObject.SetActive(false);
    }

    public void ActivateAtSpawn(Transform player)
    {
        if (_spawnArea == null)
        {
            return;
        }

        _lifecycle = EnemyLifecycle.Spawn;
        gameObject.SetActive(true);
        ResetRuntimeStatus();
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        RestoreAgentAtSpawn();
        SetTarget(player);
        _waitDespawnTimer = 0.0f;
        _leashSampleTimer = 0.0f;
        _insideLeash = true;
        _lifecycle = EnemyLifecycle.Combat;
        RestartCombatState();

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(this);
        }
    }

    public void OnReturnStateEntered()
    {
        if (_spawnArea == null || _isDead)
        {
            return;
        }

        if (_lifecycle != EnemyLifecycle.Return && _agent != null)
        {
            _storedAgentSpeed = _agent.speed;

            if (_spawnArea.ReturnMoveSpeed > 0.0f)
            {
                _agent.speed = _spawnArea.ReturnMoveSpeed;
            }
        }

        _lifecycle = EnemyLifecycle.Return;
        _waitDespawnTimer = 0.0f;
        _isCommittedAttack = false;
        ClearAwareness();
        SetTarget(null);
    }

    private void TickLeashSample()
    {
        if (_spawnArea == null || _isDead || _lifecycle == EnemyLifecycle.Inactive)
        {
            return;
        }

        _leashSampleTimer -= Time.deltaTime;

        if (_leashSampleTimer > 0.0f)
        {
            return;
        }

        _leashSampleTimer = _spawnArea.LeashCheckInterval;
        Transform player = _target != null ? _target : _spawnArea.Player;
        bool enemyInside = _spawnArea.IsInsideLeash(transform.position);
        bool playerInside = player == null || _spawnArea.IsInsideLeash(player.position);
        _insideLeash = enemyInside && playerInside;
    }

    public bool CanResumeFromLeashReturn()
    {
        return _spawnArea != null
            && _lifecycle == EnemyLifecycle.Return
            && _spawnArea.ResumeCombatDuringReturn
            && _spawnArea.IsPlayerInsideSpawn();
    }

    public void MarkCombatResumed()
    {
        _lifecycle = EnemyLifecycle.Combat;
        _waitDespawnTimer = 0.0f;
        RestoreAgentSpeed();

        if (_spawnArea != null)
        {
            SetTarget(_spawnArea.Player);
        }
    }

    public bool IsWithinReturnCompleteDistance()
    {
        float distance = _spawnArea != null ? _spawnArea.ReturnCompleteDistance : 1.0f;
        return Vector3.Distance(transform.position, _spawnPosition) <= distance;
    }

    public void CompleteLeashReturn()
    {
        if (_spawnArea == null || _isDead || _lifecycle == EnemyLifecycle.Dead)
        {
            return;
        }

        EndDirectMovement();
        StopMove();

        if (_agent != null && _agent.enabled)
        {
            _agent.ResetPath();
        }

        transform.rotation = _spawnRotation;
        _isCommittedAttack = false;
        _isDown = false;
        ClearAwareness();
        SetTarget(null);
        ResetAttackCooldown();
        RestoreAgentSpeed();

        if (_spawnArea.ResetHPOnReturn)
        {
            ResetSealHealth();

            if (characterStatus != null)
            {
                characterStatus.currentHealth = characterStatus.maxHealth;
            }
        }

        if (_spawnArea.IsPlayerInsideSpawn())
        {
            MarkCombatResumed();
            RestartCombatState();
            return;
        }

        _lifecycle = EnemyLifecycle.WaitDespawn;
        _waitDespawnTimer = 0.0f;
    }

    public void DeactivatePooled()
    {
        if (_isDead)
        {
            return;
        }

        _lifecycle = EnemyLifecycle.Inactive;
        _isCommittedAttack = false;
        _waitDespawnTimer = 0.0f;
        StopAllCoroutines();
        CancelInvoke();
        EndDirectMovement();
        StopMove();
        ClearAwareness();
        SetTarget(null);

        if (_agent != null)
        {
            if (_agent.enabled)
            {
                _agent.ResetPath();
            }

            _agent.enabled = false;
        }

        if (stateMachine != null)
        {
            stateMachine.Shutdown();
        }

        EnemyManager.Instance?.UnregisterEnemy(this);
        _spawnArea?.NotifyEnemyDeactivated();
        gameObject.SetActive(false);
    }

    protected virtual void RestartCombatState()
    {
        if (_initState != null)
        {
            ChangeEnemyState(Instantiate(_initState));
        }
    }

    private void TickWaitDespawn()
    {
        if (_spawnArea == null || _lifecycle != EnemyLifecycle.WaitDespawn || _isDead)
        {
            return;
        }

        if (_spawnArea.IsPlayerInsideSpawn())
        {
            _waitDespawnTimer = 0.0f;
            MarkCombatResumed();
            RestartCombatState();
            return;
        }

        _waitDespawnTimer += Time.deltaTime;

        if (_waitDespawnTimer >= _spawnArea.DespawnDelay)
        {
            DeactivatePooled();
        }
    }

    private void RestoreAgentAtSpawn()
    {
        if (_agent == null)
        {
            TryGetComponent(out _agent);
        }

        if (_agent == null)
        {
            return;
        }

        _agent.enabled = true;
        _agent.Warp(_spawnPosition);
        _agent.ResetPath();
        _agent.isStopped = true;
        RestoreAgentSpeed();
    }

    private void RestoreAgentSpeed()
    {
        if (_agent != null && _storedAgentSpeed > 0.0f)
        {
            _agent.speed = _storedAgentSpeed;
        }
    }

    public void ResetRuntimeStatus()
    {
        if (characterStatus != null)
        {
            characterStatus.currentHealth = characterStatus.maxHealth;
        }

        _cooldownTimer = 0.0f;
        _outsideMoveAreaTimer = 0.0f;
        _isDown = false;
        _isDead = false;
        _isDespawning = false;
        _hasNotifiedRemoved = false;
        _removalReason = EnemyRemovalReason.None;
        ClearAwareness();
        ResetSealHealth();
    }

    protected bool IsPositionInMoveArea(Vector3 position)
    {
        if (!_useMoveArea)
        {
            return true;
        }

        Vector3 center = GetMoveAreaCenter();
        Vector3 halfSize = _moveAreaSize * 0.5f;
        Vector3 local = position - center;

        return Mathf.Abs(local.x) <= halfSize.x
            && Mathf.Abs(local.y) <= halfSize.y
            && Mathf.Abs(local.z) <= halfSize.z;
    }

    public bool IsTargetInMoveArea()
    {
        return _target == null || IsPositionInMoveArea(_target.position);
    }

    public bool IsTargetWithinHomeChaseDistance()
    {
        if (_isCommittedAttack)
        {
            return true;
        }

        if (_spawnArea != null)
        {
            return _insideLeash;
        }

        if (_target == null || _maxChaseDistanceFromHome <= 0.0f)
        {
            return true;
        }

        return Vector3.Distance(_spawnPosition, _target.position) <= _maxChaseDistanceFromHome;
    }

    public bool IsTargetInSpawnArea()
    {
        return _spawnOwner == null || _spawnOwner.IsPlayerInSpawnArea(_target);
    }

    public void SetHomeDestination()
    {
        SetMoveDestination(_spawnPosition);
    }

    public bool IsAtHome()
    {
        return Vector3.Distance(transform.position, _spawnPosition) <= GetArrivalDistance();
    }

    public bool TryCorrectToNearbyNavMeshPosition()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            if (_agent != null && _agent.enabled)
            {
                _agent.Warp(hit.position);
            }
            else
            {
                transform.position = hit.position;
            }

            return true;
        }

        if (NavMesh.SamplePosition(_spawnPosition, out hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            if (_agent != null && _agent.enabled)
            {
                _agent.Warp(hit.position);
            }
            else
            {
                transform.position = hit.position;
            }

            return true;
        }

        return false;
    }

    public void BeginDirectMovement()
    {
        if (_isUsingDirectMovement)
        {
            return;
        }

        _isUsingDirectMovement = true;

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }
    }

    public void EndDirectMovement()
    {
        if (!_isUsingDirectMovement)
        {
            return;
        }

        _isUsingDirectMovement = false;

        if (_agent == null || _agent.enabled)
        {
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            _agent.enabled = true;
            return;
        }

        if (NavMesh.SamplePosition(_spawnPosition, out hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            transform.rotation = _spawnRotation;
            _agent.enabled = true;
        }
    }

    protected virtual void ReturnToSpawn()
    {
        EndDirectMovement();

        if (_agent != null && NavMesh.SamplePosition(_spawnPosition, out NavMeshHit hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            if (_agent.enabled)
            {
                _agent.Warp(hit.position);
            }
            else
            {
                transform.position = hit.position;
                _agent.enabled = true;
            }

            _agent.isStopped = true;
            _agent.ResetPath();
        }
        else
        {
            transform.position = _spawnPosition;
        }

        transform.rotation = _spawnRotation;
        _outsideMoveAreaTimer = 0.0f;
    }

    public virtual void BeginReturnToHome()
    {
        ReturnToSpawn();
    }

    public virtual void Despawn()
    {
        if (_isDead || _isDespawning)
        {
            return;
        }

        _isDespawning = true;
        _removalReason = EnemyRemovalReason.Despawned;
        CleanupForRemoval();
        Destroy(gameObject);
    }

    private void UpdateMoveAreaReturn()
    {
        if (!_useMoveArea || _isDead)
        {
            return;
        }

        if (IsPositionInMoveArea(transform.position))
        {
            _outsideMoveAreaTimer = 0.0f;
            return;
        }

        _outsideMoveAreaTimer += Time.deltaTime;

        if (_outsideMoveAreaTimer >= _returnToSpawnDelay)
        {
            ReturnToSpawn();
        }
    }

    private Vector3 GetMoveAreaCenter()
    {
        if (_moveAreaCenter != null)
        {
            return _moveAreaCenter.position + _moveAreaOffset;
        }

        Vector3 center = _hasSpawnPose ? _spawnPosition : transform.position;
        return center + _moveAreaOffset;
    }

    /// <summary>
    /// 攻撃クールダウンをリセットする
    /// </summary>
    public void ResetAttackCooldown()
    {
        _cooldownTimer = 0.0f;
    }

    /// <summary>
    /// 攻撃クールダウンの時間を進める
    /// </summary>
    public bool UpdateAttackCooldown()
    {
        _cooldownTimer += Time.deltaTime;

        return _cooldownTimer >= _attackCooldown;
    }

    protected virtual void EnterDown()
    {
        _isDown = true;
        ClearAwareness();
        StopMove();
    }

    protected virtual void RecoverFromDown()
    {
        _isDown = false;
        ResetSealHealth();
        ResumeMove();
    }

    protected virtual void FinishDown()
    {
        if (_mySealPrefab != null)
        {
            GameObject sealObject = Instantiate(_mySealPrefab, transform.position, Quaternion.identity);
            DroppingSeal droppingSeal = sealObject.GetComponent<DroppingSeal>();

            if (droppingSeal != null)
            {
                droppingSeal.SetCreateSource(SealCreateSource.Enemy);
            }
        }

        Die();
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    protected virtual void Die()
    {
        if (_isDead || _isDespawning)
        {
            return;
        }

        _isDead = true;
        _lifecycle = EnemyLifecycle.Dead;
        _isCommittedAttack = false;
        StopMove();
        _removalReason = EnemyRemovalReason.Defeated;
        ReportMissionKill();
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        NotifyRemoved();
        EnemyManager.Instance?.UnregisterEnemy(this);
    }

    private void CleanupForRemoval()
    {
        StopAllCoroutines();
        CancelInvoke();
        StopMove();

        if (stateMachine != null)
        {
            stateMachine.Shutdown();
        }

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider targetCollider in colliders)
        {
            targetCollider.enabled = false;
        }

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            audioSource.Stop();
        }
    }

    private void NotifyRemoved()
    {
        if (_hasNotifiedRemoved)
        {
            return;
        }

        _hasNotifiedRemoved = true;
        Removed?.Invoke(this);

        if (_spawnOwner != null)
        {
            _spawnOwner.NotifyEnemyRemoved(this);
            _spawnOwner = null;
        }
    }

    private float GetArrivalDistance()
    {
        if (_agent != null)
        {
            return Mathf.Max(0.2f, _agent.stoppingDistance + 0.2f);
        }

        return 0.5f;
    }

    private void InitializeRuntimeStatus()
    {
        if (characterStatus != null)
        {
            characterStatus = Instantiate(characterStatus);
        }
    }

    private void ResetSealHealth()
    {
        _currentSealHealth = Mathf.Max(1, _maxSealHealth);
    }

    public void TakeDamage(int damage)
    {
        ApplyIncomingDamage(damage);
    }

    protected virtual void ApplyIncomingDamage(int damage)
    {
        if (_isDead || _isDespawning || damage <= 0)
        {
            return;
        }

        if (_damageModel == EnemyDamageModel.Health)
        {
            ApplyHealthDamage(damage);
            return;
        }

        TakeSealHit();
    }

    private void ApplyHealthDamage(int damage)
    {
        if (characterStatus == null)
        {
            return;
        }

        characterStatus.currentHealth -= damage;

        if (characterStatus.currentHealth <= 0.0f)
        {
            Die();
        }
    }

    private void TakeSealHit()
    {
        if (_isDown)
        {
            return;
        }

        _currentSealHealth = Mathf.Max(0, _currentSealHealth - 1);

        if (_currentSealHealth <= 0)
        {
            EnterDown();
        }
    }

    /// <summary>
    /// 対象にダメージを与えられるか確認し、可能ならダメージを与える
    /// </summary>
    public bool TryAttackDamage(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return false;
        }

        Player player = targetCollider.GetComponent<Player>();

        if (player != null)
        {
            player.TryDamage(_attackDamage);

            return true;
        }

        IDamageable damageable = targetCollider.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable = targetCollider.GetComponentInParent<IDamageable>();
        }

        if (damageable == null || ReferenceEquals(damageable, this))
        {
            return false;
        }

        damageable.TakeDamage(_attackDamage);
        return true;
    }

    /// <summary>
    /// シール攻撃とフィニッシャーのヒット処理
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other == null || _isDead || _isDespawning)
        {
            return;
        }

        HandleIncomingAttack(other);
    }

    protected virtual void HandleIncomingAttack(Collider other)
    {
        bool isSealAttack = HasTag(other, _sealAttackTag);
        bool isFinisher = IsFinisherAttack(other);

        if (_damageModel == EnemyDamageModel.Seal && _isDown)
        {
            if (isFinisher || (_treatSealAttackAsFinisherWhenDown && isSealAttack))
            {
                Destroy(other.gameObject);
                FinishDown();
            }

            return;
        }

        if (!isSealAttack)
        {
            return;
        }

        Destroy(other.gameObject);
        ApplyIncomingDamage(1);
    }

    private bool IsFinisherAttack(Collider targetCollider)
    {
        if (HasTag(targetCollider, _finisherTag))
        {
            return true;
        }

        return targetCollider.GetComponent<EnemyFinisherMarker>() != null
            || targetCollider.GetComponentInParent<EnemyFinisherMarker>() != null;
    }

    private bool HasTag(Collider targetCollider, string tagName)
    {
        return !string.IsNullOrEmpty(tagName) && targetCollider.gameObject.tag == tagName;
    }

    private void ReportMissionKill()
    {
        if (_enemyData == null)
        {
            return;
        }

        Missionmanager mission = Missionmanager.Instance;

        if (mission == null || mission.targetObject == null)
        {
            return;
        }

        mission.AddKill(_enemyData);
    }

    protected virtual float GetPerceptionRange()
    {
        return _alertDistance;
    }

    protected virtual float GetAlertConfirmTime()
    {
        return _alertConfirmTime;
    }

    protected virtual float GetForcedPerceptionRange()
    {
        return 0.0f;
    }

    private void TickPerception()
    {
        if (_isDead || _isDespawning || _isDown)
        {
            return;
        }

        if (!HasTarget())
        {
            ClearAwareness();
            return;
        }

        if (_awareness == EnemyAwareness.Engaged)
        {
            UpdateEngagedPerception();
            return;
        }

        if (!CanSeeTarget())
        {
            ClearAwareness();
            return;
        }

        if (_awareness == EnemyAwareness.Unaware)
        {
            _awareness = EnemyAwareness.Alert;
            _awarenessTimer = 0.0f;
            return;
        }

        _awarenessTimer += Time.deltaTime;

        if (_awarenessTimer >= GetAlertConfirmTime())
        {
            _awareness = EnemyAwareness.Engaged;
            _lostSightTimer = 0.0f;
        }
    }

    private void UpdateEngagedPerception()
    {
        if (!IsTargetWithinHomeChaseDistance() || IsTargetLost())
        {
            ClearAwareness();
            return;
        }

        if (HasLineOfSightToTarget())
        {
            _lostSightTimer = 0.0f;
            return;
        }

        _lostSightTimer += Time.deltaTime;

        if (_lostSightTimer >= _sightMemoryDuration)
        {
            ClearAwareness();
        }
    }

    private bool CanSeeTarget()
    {
        return IsInPerceptionDistance() && IsTargetInFieldOfView() && HasLineOfSightToTarget();
    }

    private bool IsInPerceptionDistance()
    {
        if (!HasTarget() || !IsTargetWithinHomeChaseDistance())
        {
            return false;
        }

        float forcedRange = GetForcedPerceptionRange();

        if (forcedRange > 0.0f && IsTargetInDistance(forcedRange))
        {
            return true;
        }

        if (!IsTargetInMoveArea())
        {
            return false;
        }

        return IsTargetInDistance(GetPerceptionRange());
    }

    private void ClearAwareness()
    {
        _awareness = EnemyAwareness.Unaware;
        _awarenessTimer = 0.0f;
        _lostSightTimer = 0.0f;
    }

    private Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * _eyeHeight;
    }

    private Vector3 GetFlatForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0.0f;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    private bool IsOwnCollider(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return false;
        }

        Transform hitTransform = targetCollider.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private bool IsTargetCollider(Collider targetCollider)
    {
        if (_target == null || targetCollider == null)
        {
            return false;
        }

        Transform hitTransform = targetCollider.transform;
        return hitTransform == _target
            || hitTransform.IsChildOf(_target)
            || _target.IsChildOf(hitTransform);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * _eyeHeight;
        Vector3 forward = transform.forward;
        forward.y = 0.0f;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        float range = Mathf.Max(0.1f, _alertDistance);
        Quaternion left = Quaternion.AngleAxis(-_viewAngle * 0.5f, Vector3.up);
        Quaternion right = Quaternion.AngleAxis(_viewAngle * 0.5f, Vector3.up);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eye, eye + left * forward * range);
        Gizmos.DrawLine(eye, eye + right * forward * range);
        Gizmos.DrawLine(eye, eye + forward * range);
    }
}
