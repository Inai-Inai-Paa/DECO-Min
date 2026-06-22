using UnityEngine;

/// <summary>
/// 敵UI用の基底クラス。
/// </summary>
public class UIEnemyBase : UIBase
{
    [Header("Enemy UI Base")]

    [SerializeField]
    protected CharacterStatus _enemyStatus;

    protected override void Start()
    {
        Show();

        if (_enemyStatus == null)
        {
            Debug.LogWarning($"{nameof(UIEnemyBase)} : EnemyStatus が設定されていません。Inspectorに敵のCharacterStatusを入れて。");
        }
    }
}