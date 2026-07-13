using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Tackle")]
public class TackleEnemyTackle : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _cooldownState;

    private Vector3 _tackleDirection;
    private float _tackleMoveDistance;
    private bool _hasHitTarget;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        tackleEnemy.BeginDirectMovementForState();
        _tackleMoveDistance = 0.0f;
        _hasHitTarget = false;
        _tackleDirection = tackleEnemy.GetDirectionToTargetForState();
    }

    public override void Exit()
    {
        tackleEnemy.EndDirectMovementForState();
    }

    public override void Update()
    {
        if (!tackleEnemy.IsTargetWithinHomeChaseDistanceForState())
        {
            ChangeTackleState<TackleEnemyReturn>(null);
            return;
        }

        float moveDistance = tackleEnemy.TackleSpeed * Time.deltaTime;
        Vector3 moveValue = _tackleDirection * moveDistance;

        if (tackleEnemy.IsTackleBlocked(_tackleDirection, moveDistance))
        {
            ChangeTackleState<TackleEnemyCooldown>(_cooldownState);
            return;
        }

        tackleEnemy.MoveDirectForState(moveValue);
        _tackleMoveDistance += moveDistance;

        if (!_hasHitTarget && tackleEnemy.TryTackleHit())
        {
            _hasHitTarget = true;
            ChangeTackleState<TackleEnemyCooldown>(_cooldownState);
            return;
        }

        if (_tackleMoveDistance >= tackleEnemy.TackleDistance)
        {
            ChangeTackleState<TackleEnemyCooldown>(_cooldownState);
        }
    }
}
