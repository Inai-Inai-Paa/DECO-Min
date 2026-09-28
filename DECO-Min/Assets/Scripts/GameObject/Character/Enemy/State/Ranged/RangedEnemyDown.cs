using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Ranged/Down")]
public class RangedEnemyDown : RangedEnemyState
{
    private float _downTimer;

    public override void Enter()
    {
        rangedEnemy.StopMove();
        _downTimer = 0.0f;
    }

    public override void Update()
    {
        rangedEnemy.StopMove();
        _downTimer += Time.deltaTime;

        if (_downTimer >= rangedEnemy.DownRecoverTime)
        {
            rangedEnemy.RecoverFromDownForState();
        }
    }
}
