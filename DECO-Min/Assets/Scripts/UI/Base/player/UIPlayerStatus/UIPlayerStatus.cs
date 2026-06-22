using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤー自身のHPゲージを表示するUI。
/// Imageは自分のGameObjectから取得する。
/// </summary>
public class UIPlayerHealthGauge : UIPlayerBase
{
    private Image _healthGauge;

    protected override void Start()
    {
        base.Start();

        _healthGauge = GetComponent<Image>();

        if (_healthGauge == null)
        {
            Debug.LogWarning($"{nameof(UIPlayerHealthGauge)} : Image が見つかりません。同じGameObjectに付けてください。");
        }
    }

    private void Update()
    {
        if (_playerStatus == null)
        {
            return;
        }

        if (_healthGauge == null)
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

        _healthGauge.fillAmount = healthRate;
    }
}