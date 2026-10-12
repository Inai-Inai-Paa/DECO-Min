using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Cooldown")]
public class TackleEnemyCooldown : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _moveAreaReturnState;

    private float _waitTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        _waitTimer = 0.0f;
    }

    public override void Update()
    {
        _waitTimer += Time.deltaTime;

        if (_waitTimer < tackleEnemy.PostTackleWait)
        {
            return;
        }

        if (!tackleEnemy.IsSelfInsideMoveArea())
        {
            if (tackleEnemy._animator != null)
            {
                tackleEnemy._animator.SetTrigger("Return");
            }
            ChangeTackleState<TackleEnemyMoveAreaReturn>(_moveAreaReturnState);
            return;
        }

        if (tackleEnemy.IsTargetStillVisibleForTackle())
        {
            ChangeTackleState<TackleEnemyAlert>(null);
            return;
        }

        ChangeTackleState<TackleEnemyPatrol>(_patrolState);
    }
}
