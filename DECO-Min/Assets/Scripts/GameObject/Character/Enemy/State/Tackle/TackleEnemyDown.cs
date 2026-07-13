using UnityEngine;

[CreateAssetMenu(menuName = "State/Enemy/Tackle/Down")]
public class TackleEnemyDown : TackleEnemyState
{
    private float _downTimer;

    public override void Enter()
    {
        tackleEnemy.StopMoveForState();
        _downTimer = 0.0f;
    }

    public override void Update()
    {
        tackleEnemy.StopMoveForState();
        _downTimer += Time.deltaTime;

        if (_downTimer >= tackleEnemy.DownRecoverTime)
        {
            tackleEnemy.RecoverFromDownForState();
        }
    }
}
