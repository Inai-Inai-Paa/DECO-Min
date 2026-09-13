using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Charge")]
public class TackleEnemyCharge : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _tackleState;

    private float _chargeTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        _chargeTimer = 0.0f;
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

        tackleEnemy.LookAtTargetForState();

        _chargeTimer += Time.deltaTime;

        if (_chargeTimer >= tackleEnemy.ChargeTime)
        {
            ChangeTackleState<TackleEnemyTackle>(_tackleState);
        }
    }
}
