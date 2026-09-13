using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Cooldown")]
public class TackleEnemyCooldown : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _chaseState;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        tackleEnemy.ResetAttackCooldownForState();
    }

    public override void Update()
    {
        if (!tackleEnemy.IsTargetWithinHomeChaseDistanceForState())
        {
            ChangeTackleState<TackleEnemyReturn>(null);
            return;
        }

        if (!tackleEnemy.UpdateAttackCooldownForState())
        {
            return;
        }

        if (tackleEnemy.HasTargetForState() && tackleEnemy.IsTargetInAlertDistanceForState())
        {
            ChangeTackleState<TackleEnemyChase>(_chaseState);
        }
        else
        {
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
        }
    }
}
