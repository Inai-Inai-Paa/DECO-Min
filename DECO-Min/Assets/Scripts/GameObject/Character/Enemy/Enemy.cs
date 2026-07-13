using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/// <summary>
/// 敵キャラクターの共通処理を管理する基底クラス
/// </summary>
public partial class Enemy : Character
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

    public bool IsGrounded => _isGrounded;
    public bool IsDown => _isDown;
    public bool IsDead => _isDead;
    public bool IsDespawning => _isDespawning;
    public Vector3 HomePosition => _spawnPosition;
    public Quaternion HomeRotation => _spawnRotation;
    public float ReturnImpossibleTime => _returnImpossibleTime;
    public float HomeDespawnWaitTime => _homeDespawnWaitTime;
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
        base.Update();
        UpdateMoveAreaReturn();
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
    protected bool HasTarget()
    {
        return _target != null;
    }

    /// <summary>
    /// ターゲットとの距離を取得する
    /// </summary>
    protected float GetDistanceToTarget()
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
    protected bool IsTargetInDistance(float distance)
    {
        return GetDistanceToTarget() <= distance;
    }

    /// <summary>
    /// ターゲットが警戒距離内にいるか確認する
    /// </summary>
    protected bool IsTargetInAlertDistance()
    {
        return IsTargetInDistance(_alertDistance);
    }

    /// <summary>
    /// ターゲットを見失ったか確認する
    /// </summary>
    protected bool IsTargetLost()
    {
        return GetDistanceToTarget() >= _lostDistance;
    }

    /// <summary>
    /// ターゲット方向の水平ベクトルを取得する
    /// </summary>
    protected Vector3 GetDirectionToTarget()
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
    protected void LookAtTarget(float rotateSpeed = 10.0f)
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
    protected void SetMoveDestination(Vector3 destination)
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
    protected void StopMove()
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
    protected void ResumeMove()
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
    protected void MoveDirect(Vector3 moveValue)
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
    protected bool IsArrived()
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

    protected bool IsTargetInMoveArea()
    {
        return _target == null || IsPositionInMoveArea(_target.position);
    }

    protected bool IsTargetWithinHomeChaseDistance()
    {
        if (_target == null || _maxChaseDistanceFromHome <= 0.0f)
        {
            return true;
        }

        return Vector3.Distance(_spawnPosition, _target.position) <= _maxChaseDistanceFromHome;
    }

    protected bool IsTargetInSpawnArea()
    {
        return _spawnOwner == null || _spawnOwner.IsPlayerInSpawnArea(_target);
    }

    protected void SetHomeDestination()
    {
        SetMoveDestination(_spawnPosition);
    }

    protected bool IsAtHome()
    {
        return Vector3.Distance(transform.position, _spawnPosition) <= GetArrivalDistance();
    }

    protected bool TryCorrectToNearbyNavMeshPosition()
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

    protected void BeginDirectMovement()
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

    protected void EndDirectMovement()
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
    protected void ResetAttackCooldown()
    {
        _cooldownTimer = 0.0f;
    }

    /// <summary>
    /// 攻撃クールダウンの時間を進める
    /// </summary>
    protected bool UpdateAttackCooldown()
    {
        _cooldownTimer += Time.deltaTime;

        return _cooldownTimer >= _attackCooldown;
    }

    protected virtual void EnterDown()
    {
        _isDown = true;
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
        if (_isDead)
        {
            return;
        }

        _isDead = true;
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
    protected bool TryAttackDamage(Collider targetCollider)
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

        if (damageable == null)
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
        if (other == null)
        {
            return;
        }

        bool isSealAttack = HasTag(other, _sealAttackTag);
        bool isFinisher = HasTag(other, _finisherTag);

        if (_isDown)
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
        TakeSealHit();
    }

    private bool HasTag(Collider targetCollider, string tagName)
    {
        return !string.IsNullOrEmpty(tagName) && targetCollider.gameObject.tag == tagName;
    }
}
