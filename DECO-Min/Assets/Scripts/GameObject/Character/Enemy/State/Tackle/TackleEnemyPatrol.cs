using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Patrol")]
public class TackleEnemyPatrol : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _alertState;

    private float _patrolTimer;

    public override void Enter()
    {
        tackleEnemy.ResumeMoveForState();
        _patrolTimer = 0.0f;
        tackleEnemy.SetRandomPatrolPoint();
    }

    public override void Update()
    {
        if (!tackleEnemy.HasTargetForState())
        {
            return;
        }

        if (tackleEnemy.IsTargetInAlertDistanceForState())
        {
            ChangeTackleState<TackleEnemyAlert>(_alertState);
            return;
        }

        _patrolTimer += Time.deltaTime;

        if (_patrolTimer >= tackleEnemy.PatrolPointInterval || tackleEnemy.IsArrivedForState())
        {
            _patrolTimer = 0.0f;
            tackleEnemy.SetRandomPatrolPoint();
        }
    }
}
