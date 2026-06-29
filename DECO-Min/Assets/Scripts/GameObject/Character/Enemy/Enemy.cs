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

    [Header("攻撃共通")]
    [Space(2)]
    [SerializeField]
    protected int _attackDamage = 10;
    [SerializeField]
    protected float _attackCooldown = 1.5f;

    protected float _cooldownTimer;

    [SerializeField] private GameObject _mySealPrefab;

    private bool _isGrounded;

    public bool IsGrounded => _isGrounded;

    protected override void Start()
    {
        base.Start();

        InitializeEnemyReferences();

        if (_initState != null)
        {
            ChangeEnemyState(_initState);
        }
    }

    protected override void Update()
    {
        base.Update();
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

        _agent.isStopped = false;
        _agent.SetDestination(destination);
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
        if (_agent != null && _agent.enabled)
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

        //この下のコードの意図がわからなかったので後で聞きます
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
    /// 一旦のプロトまでなので後で絶対変えて！忘れてたら教えて！
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("PlayerAttack"))
        {
            Instantiate(_mySealPrefab, transform.position, Quaternion.identity);

            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}
