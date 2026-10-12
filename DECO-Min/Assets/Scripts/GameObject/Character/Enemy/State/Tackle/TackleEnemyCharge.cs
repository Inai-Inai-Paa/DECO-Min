using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Charge")]
public class TackleEnemyCharge : TackleEnemyState
{
    [Header("TransitionState")]
    [SerializeField] private EnemyState _tackleState;

    private float _windupTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        tackleEnemy.LockTackleAim();
        _windupTimer = 0.0f;
    }

    public override void Update()
    {
        _windupTimer += Time.deltaTime;

        if (_windupTimer < tackleEnemy.WindupTime)
        {
            return;
        }

        if (tackleEnemy._animator != null)
        {
            tackleEnemy._animator.SetTrigger("Tackle");
        }
        ChangeTackleState<TackleEnemyTackle>(_tackleState);
    }
}
