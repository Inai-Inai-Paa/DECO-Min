using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Chase")]
public class TackleEnemyChase : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _chargeState;

    public override void Enter()
    {
        tackleEnemy.ResumeMoveForState();
    }

    public override void Update()
    {
        if (!tackleEnemy.IsTargetWithinHomeChaseDistanceForState())
        {
            ChangeTackleState<TackleEnemyReturn>(null);
            return;
        }

        if (!tackleEnemy.HasTargetForState() || tackleEnemy.IsTargetLostForState())
        {
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
            return;
        }

        if (tackleEnemy.IsTargetInAttackStartDistance())
        {
            ChangeTackleState<TackleEnemyCharge>(_chargeState);
            return;
        }

        tackleEnemy.SetTargetDestination();
    }
}
