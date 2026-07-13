using UnityEngine;

public abstract class TackleEnemyState : EnemyState
{
    protected TackleEnemy tackleEnemy => enemy as TackleEnemy;

    protected void ChangeTackleState<T>(EnemyState stateTemplate)
        where T : EnemyState
    {
        EnemyState nextState = stateTemplate != null
            ? Instantiate(stateTemplate)
            : CreateInstance<T>();

        enemy.ChangeEnemyState(nextState);
    }
}
