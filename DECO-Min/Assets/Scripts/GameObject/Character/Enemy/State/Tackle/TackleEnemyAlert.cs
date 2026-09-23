using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Alert")]
public class TackleEnemyAlert : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _chaseState;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
    }

    public override void Update()
    {
        if (!tackleEnemy.IsTargetWithinHomeChaseDistanceForState())
        {
            ChangeTackleState<TackleEnemyReturn>(null);
            return;
        }

        tackleEnemy.LookAtTargetForState();

        if (tackleEnemy.Awareness == EnemyAwareness.Engaged)
        {
            ChangeTackleState<TackleEnemyChase>(_chaseState);
            return;
        }

        if (tackleEnemy.Awareness == EnemyAwareness.Unaware)
        {
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
        }
    }
}
