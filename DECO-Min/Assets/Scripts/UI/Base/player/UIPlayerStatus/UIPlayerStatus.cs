using UnityEngine;

/// <summary>
/// プレイヤー自身のHPをハートアイコンで表示するUI。
/// </summary>
public class UIPlayerHealthGauge : UIPlayerBase
{
    [SerializeField] private UIHealthHeart[] _hearts;

    protected override void Start()
    {
        base.Start();

        if (_hearts == null || _hearts.Length == 0)
        {
            Debug.LogWarning($"{nameof(UIPlayerHealthGauge)} : ハートUIが設定されていません。");
        }
    }

    private void Update()
    {
        if (!IsValidPlayerStatus())
        {
            return;
        }

        if (_hearts == null || _hearts.Length == 0)
        {
            return;
        }

        float healthRate = 0.0f;

        if (_playerStatus.maxHealth > 0.0f)
        {
            healthRate = Mathf.Clamp01(
                _playerStatus.currentHealth / _playerStatus.maxHealth
            );
        }

        int halfHeartCount = Mathf.FloorToInt(
            healthRate * _hearts.Length * 2.0f
        );

        for (int i = 0; i < _hearts.Length; i++)
        {
            int heartHalfCount = Mathf.Clamp(
                halfHeartCount - i * 2,
                0,
                2
            );

            _hearts[i].SetHealth(heartHalfCount * 0.5f);
        }
    }
}