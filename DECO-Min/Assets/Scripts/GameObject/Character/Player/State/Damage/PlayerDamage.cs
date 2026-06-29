using UnityEngine;

[CreateAssetMenu(menuName = "State/Player/Damage")]
public class PlayerDamage : PlayerState
{
    [Header("遷移先ステート")]
    [SerializeField]
    private PlayerState _nextState;

    [Header("無敵時間")]
    [SerializeField]
    private float _invincibleTime;

    public override void Enter()
    {
        player.ApplyDamage(player.pendingDamage);
        player.StartInvincible(_invincibleTime);    // 無敵時間開始
        player.ChangePlayerState(Instantiate(_nextState));
    }
}
