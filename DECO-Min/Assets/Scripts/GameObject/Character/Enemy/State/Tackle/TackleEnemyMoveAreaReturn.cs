using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/MoveAreaReturn")]
public class TackleEnemyMoveAreaReturn : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;

    public override void Enter()
    {
        tackleEnemy.EndDirectMovementForState();
        tackleEnemy.ClearAwarenessForState();
        tackleEnemy.ApplyPatrolSpeed();
        tackleEnemy.SetAgentRotationUpdates(true);
        tackleEnemy.ResumeMoveForState();
        tackleEnemy.SetHomeDestinationForState();
    }

    public override void Update()
    {
        if (tackleEnemy.IsSelfInsideMoveArea())
        {
            tackleEnemy.StopMoveForState();
            tackleEnemy.ClearAwarenessForState();
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
            return;
        }

        tackleEnemy.ApplyPatrolSpeed();
        tackleEnemy.ResumeMoveForState();
        tackleEnemy.SetHomeDestinationForState();
    }
}
