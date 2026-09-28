using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Charge")]
public class TackleEnemyCharge : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _patrolState;
    [SerializeField] private EnemyState _tackleState;

    private float _chargeTimer;
    private bool _isAimLocked;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        tackleEnemy.ClearLockedTackleDirection();
        _chargeTimer = 0.0f;
        _isAimLocked = false;
    }

    public override void Update()
    {
        if (!tackleEnemy.IsTargetWithinHomeChaseDistanceForState())
        {
            tackleEnemy._animator.SetTrigger("Return");
            ChangeTackleState<TackleEnemyReturn>(null);
            return;
        }

        if (!tackleEnemy.HasTargetForState() || tackleEnemy.IsTargetLostForState())
        {
            tackleEnemy._animator.SetTrigger("Return");
            ChangeTackleState<TackleEnemyPatrol>(_patrolState);
            return;
        }

        _chargeTimer += Time.deltaTime;

        if (!_isAimLocked && _chargeTimer >= tackleEnemy.ChargeTime - tackleEnemy.AimLockLeadTime)
        {
            tackleEnemy.LockTackleAim();
            _isAimLocked = true;
        }

        if (!_isAimLocked)
        {
            tackleEnemy.LookAtTargetForState();
        }

        if (_chargeTimer >= tackleEnemy.ChargeTime)
        {
            tackleEnemy._animator.SetTrigger("Tackle");
            ChangeTackleState<TackleEnemyTackle>(_tackleState);
        }
    }
}
