using UnityEngine;

public abstract class RangedEnemyState : EnemyState
{
    protected RangedEnemy rangedEnemy => enemy as RangedEnemy;

    protected void ChangeRangedState<T>(EnemyState stateTemplate)
        where T : EnemyState
    {
        EnemyState nextState = stateTemplate != null
            ? Instantiate(stateTemplate)
            : CreateInstance<T>();

        enemy.ChangeEnemyState(nextState);
    }
}
