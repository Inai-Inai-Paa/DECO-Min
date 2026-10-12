using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Alert")]
public class TackleEnemyAlert : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _chargeState;

    private float _aimTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        tackleEnemy.ClearLockedTackleDirection();
        tackleEnemy.RememberVisibleAimPoint();
        _aimTimer = 0.0f;
    }

    public override void Update()
    {
        tackleEnemy.RememberVisibleAimPoint();

        if (tackleEnemy.HasTargetForState() && tackleEnemy.HasLineOfSightToTarget())
        {
            tackleEnemy.LookAtTargetForState();
        }
        else if (tackleEnemy.HasAimPoint)
        {
            tackleEnemy.LookAtHorizontalPoint(tackleEnemy.AimPoint);
        }

        _aimTimer += Time.deltaTime;

        if (_aimTimer < tackleEnemy.AimFollowTime)
        {
            return;
        }

        tackleEnemy.LockTackleAim();

        if (tackleEnemy._animator != null)
        {
            tackleEnemy._animator.SetTrigger("Charge");
        }
        ChangeTackleState<TackleEnemyCharge>(_chargeState);
    }
}
