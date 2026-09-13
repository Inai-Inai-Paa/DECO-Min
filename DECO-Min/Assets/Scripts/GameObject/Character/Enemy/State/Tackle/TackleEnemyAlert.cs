using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Alert")]
public class TackleEnemyAlert : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _chaseState;

    private float _alertTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        _alertTimer = 0.0f;
    }

    public override void Update()
    {
        if (!tackleEnemy.IsTargetWithinHomeChaseDistanceForState())
        {
            ChangeTackleState<TackleEnemyReturn>(null);
            return;
        }

        if (!tackleEnemy.HasTargetForState())
        {
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
            return;
        }

        tackleEnemy.LookAtTargetForState();

        if (!tackleEnemy.IsTargetInAlertDistanceForState())
        {
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
            return;
        }

        _alertTimer += Time.deltaTime;

        if (_alertTimer >= tackleEnemy.AlertTime)
        {
            ChangeTackleState<TackleEnemyChase>(_chaseState);
        }
    }
}
