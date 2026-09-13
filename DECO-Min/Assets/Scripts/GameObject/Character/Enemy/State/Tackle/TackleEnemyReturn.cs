using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Return")]
public class TackleEnemyReturn : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _chaseState;
    [SerializeField] private EnemyState _chargeState;

    private float _returnTimer;
    private float _homeWaitTimer;
    private int _returnRetryCount;
    private bool _isWaitingAtHome;

    public override void Enter()
    {
        tackleEnemy.EndDirectMovementForState();
        tackleEnemy.ResumeMoveForState();
        tackleEnemy.SetHomeDestinationForState();
        _returnTimer = 0.0f;
        _homeWaitTimer = 0.0f;
        _returnRetryCount = 0;
        _isWaitingAtHome = false;
    }

    public override void Update()
    {
        if (CanResumeCombat())
        {
            if (tackleEnemy.IsTargetInAttackStartDistance())
            {
                ChangeTackleState<TackleEnemyCharge>(_chargeState);
            }
            else
            {
                ChangeTackleState<TackleEnemyChase>(_chaseState);
            }

            return;
        }

        if (_isWaitingAtHome)
        {
            UpdateHomeWait();
            return;
        }

        _returnTimer += Time.deltaTime;

        if (tackleEnemy.IsAtHomeForState() || tackleEnemy.IsArrivedForState())
        {
            BeginHomeWait();
            return;
        }

        if (_returnTimer >= tackleEnemy.ReturnImpossibleTime)
        {
            if (_returnRetryCount == 0 && tackleEnemy.TryCorrectToNearbyNavMeshPositionForState())
            {
                tackleEnemy.SetHomeDestinationForState();
                _returnTimer = 0.0f;
                _returnRetryCount++;
                return;
            }

            tackleEnemy.Despawn();
            return;
        }

        tackleEnemy.SetHomeDestinationForState();
    }

    private bool CanResumeCombat()
    {
        return tackleEnemy.HasTargetForState()
            && tackleEnemy.IsTargetWithinHomeChaseDistanceForState()
            && tackleEnemy.IsTargetInAlertDistanceForState();
    }

    private void BeginHomeWait()
    {
        _isWaitingAtHome = true;
        _homeWaitTimer = 0.0f;
        tackleEnemy.StopMoveForState();
    }

    private void UpdateHomeWait()
    {
        if (tackleEnemy.IsTargetInSpawnAreaForState())
        {
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
            return;
        }

        _homeWaitTimer += Time.deltaTime;

        if (_homeWaitTimer >= tackleEnemy.HomeDespawnWaitTime)
        {
            tackleEnemy.Despawn();
        }
    }
}
