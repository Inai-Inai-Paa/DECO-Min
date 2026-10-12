using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Patrol")]
public class TackleEnemyPatrol : TackleEnemyState
{
    private enum Phase
    {
        Wait,
        Turn,
        Move
    }

    [Header("TransitionState")]
    [SerializeField] private EnemyState _alertState;

    private Phase _phase;
    private float _timer;
    private Vector3 _destination;
    private Quaternion _turnStart;
    private Quaternion _turnEnd;

    public override void Enter()
    {
        tackleEnemy.ClearAwarenessForState();
        tackleEnemy.ReleasePlayerBodyCollision();
        tackleEnemy.ApplyPatrolSpeed();
        tackleEnemy.SetAgentRotationUpdates(false);
        tackleEnemy.StopMoveForState();
        _timer = 0.0f;
        _phase = Phase.Wait;

        if (!tackleEnemy.TryChoosePatrolDestination(out _destination))
        {
            _destination = tackleEnemy.transform.position;
        }
    }

    public override void Exit()
    {
        tackleEnemy.SetAgentRotationUpdates(true);
    }

    public override void Update()
    {
        if (tackleEnemy.IsTargetVisibleForTackle())
        {
            if (tackleEnemy._animator != null)
            {
                tackleEnemy._animator.SetTrigger("Find");
            }
            ChangeTackleState<TackleEnemyAlert>(_alertState);
            return;
        }

        switch (_phase)
        {
            case Phase.Wait:
                tackleEnemy.StopMoveForState();
                _timer += Time.deltaTime;

                if (_timer >= tackleEnemy.PatrolTurnWait)
                {
                    BeginTurn();
                }

                break;

            case Phase.Turn:
                _timer += Time.deltaTime;
                float turn = Mathf.Clamp01(_timer / tackleEnemy.PatrolTurnDuration);
                tackleEnemy.transform.rotation = Quaternion.Slerp(_turnStart, _turnEnd, turn);

                if (turn >= 1.0f)
                {
                    tackleEnemy.BeginPatrolMove(_destination);
                    _phase = Phase.Move;
                }

                break;

            case Phase.Move:
                if (tackleEnemy.HasReachedPatrolPoint(_destination))
                {
                    tackleEnemy.StopMoveForState();

                    if (!tackleEnemy.TryChoosePatrolDestination(out _destination))
                    {
                        _destination = tackleEnemy.transform.position;
                    }

                    _timer = 0.0f;
                    _phase = Phase.Wait;
                }

                break;
        }
    }

    private void BeginTurn()
    {
        _phase = Phase.Turn;
        _timer = 0.0f;
        _turnStart = tackleEnemy.transform.rotation;
        Vector3 direction = _destination - tackleEnemy.transform.position;
        direction.y = 0.0f;
        _turnEnd = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized)
            : _turnStart;
    }
}
