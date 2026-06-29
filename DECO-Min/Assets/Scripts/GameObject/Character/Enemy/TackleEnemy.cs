using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 一定範囲を徘徊し、プレイヤーを発見するとチャージタックル攻撃を行う敵AI
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TackleEnemy : Enemy
{
    private enum TackleEnemyState
    {
        Patrol,
        Alert,
        Chase,
        Charge,
        Tackle,
        Cooldown
    }

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

    private TackleEnemyState _currentState = TackleEnemyState.Patrol;

    private float _patrolTimer;
    private float _alertTimer;
    private float _chargeTimer;
    private float _tackleMoveDistance;

    private Vector3 _tackleDirection;

    private bool _hasHitTarget;

    protected override void Start()
    {
        base.Start();

        if (_patrolCenter == null)
        {
            _patrolCenter = transform;
        }

        ChangeAIState(TackleEnemyState.Patrol);
    }

    protected override void Update()
    {
        base.Update();

        if (!HasTarget())
        {
            return;
        }

        switch (_currentState)
        {
            case TackleEnemyState.Patrol:
                UpdatePatrol();
                break;

            case TackleEnemyState.Alert:
                UpdateAlert();
                break;

            case TackleEnemyState.Chase:
                UpdateChase();
                break;

            case TackleEnemyState.Charge:
                UpdateCharge();
                break;

            case TackleEnemyState.Tackle:
                UpdateTackle();
                break;

            case TackleEnemyState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    /// <summary>
    /// タックル敵用の状態を変更する
    /// </summary>
    private void ChangeAIState(TackleEnemyState nextState)
    {
        _currentState = nextState;

        switch (_currentState)
        {
            case TackleEnemyState.Patrol:
                ResumeMove();
                _patrolTimer = 0.0f;
                SetRandomPatrolPoint();
                break;

            case TackleEnemyState.Alert:
                StopMove();
                _alertTimer = 0.0f;
                break;

            case TackleEnemyState.Chase:
                ResumeMove();
                break;

            case TackleEnemyState.Charge:
                StopMove();
                _chargeTimer = 0.0f;
                break;

            case TackleEnemyState.Tackle:
                StopMove();
                _tackleMoveDistance = 0.0f;
                _hasHitTarget = false;
                _tackleDirection = GetDirectionToTarget();
                break;

            case TackleEnemyState.Cooldown:
                StopMove();
                ResetAttackCooldown();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (IsTargetInAlertDistance())
        {
            ChangeAIState(TackleEnemyState.Alert);
            return;
        }

        _patrolTimer += Time.deltaTime;

        if (_patrolTimer >= _patrolPointInterval || IsArrived())
        {
            _patrolTimer = 0.0f;
            SetRandomPatrolPoint();
        }
    }

    private void UpdateAlert()
    {
        LookAtTarget();

        if (!IsTargetInAlertDistance())
        {
            ChangeAIState(TackleEnemyState.Patrol);
            return;
        }

        _alertTimer += Time.deltaTime;

        if (_alertTimer >= _alertTime)
        {
            ChangeAIState(TackleEnemyState.Chase);
        }
    }

    private void UpdateChase()
    {
        if (IsTargetLost())
        {
            ChangeAIState(TackleEnemyState.Patrol);
            return;
        }

        if (IsTargetInDistance(_attackStartDistance))
        {
            ChangeAIState(TackleEnemyState.Charge);
            return;
        }

        SetMoveDestination(_target.position);
    }

    private void UpdateCharge()
    {
        LookAtTarget();

        if (IsTargetLost())
        {
            ChangeAIState(TackleEnemyState.Patrol);
            return;
        }

        _chargeTimer += Time.deltaTime;

        if (_chargeTimer >= _chargeTime)
        {
            ChangeAIState(TackleEnemyState.Tackle);
        }
    }

    private void UpdateTackle()
    {
        float moveDistance = _tackleSpeed * Time.deltaTime;
        Vector3 moveValue = _tackleDirection * moveDistance;

        // 壁などに当たりそうならタックル終了
        if (Physics.Raycast(transform.position, _tackleDirection, moveDistance, _obstacleLayer))
        {
            ChangeAIState(TackleEnemyState.Cooldown);
            return;
        }

        MoveDirect(moveValue);
        _tackleMoveDistance += moveDistance;

        CheckTackleHit();

        if (_tackleMoveDistance >= _tackleDistance)
        {
            ChangeAIState(TackleEnemyState.Cooldown);
        }
    }

    private void UpdateCooldown()
    {
        if (!UpdateAttackCooldown())
        {
            return;
        }

        if (IsTargetInAlertDistance())
        {
            ChangeAIState(TackleEnemyState.Chase);
        }
        else
        {
            ChangeAIState(TackleEnemyState.Patrol);
        }
    }

    private void SetRandomPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * _patrolRadius;
        Vector3 randomPoint = _patrolCenter.position + new Vector3(randomCircle.x, 0.0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            SetMoveDestination(hit.position);
        }
    }

    private void CheckTackleHit()
    {
        if (_hasHitTarget)
        {
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            _tackleHitRadius,
            _targetLayer
        );

        foreach (Collider hitCollider in hitColliders)
        {
            if (TryAttackDamage(hitCollider))
            {
                _hasHitTarget = true;
                ChangeAIState(TackleEnemyState.Cooldown);
                return;
            }
        }
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
