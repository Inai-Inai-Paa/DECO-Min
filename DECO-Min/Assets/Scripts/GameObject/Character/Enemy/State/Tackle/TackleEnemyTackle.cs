using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Tackle")]
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
            ChangeTackleState<TackleEnemyCooldown>(_cooldownState);
            return;
        }

        float moveDistance = Mathf.Min(tackleEnemy.TackleSpeed * Time.deltaTime, remaining);

        if (tackleEnemy.IsTackleBlocked(_tackleDirection, moveDistance))
        {
            ChangeTackleState<TackleEnemyCooldown>(_cooldownState);
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
