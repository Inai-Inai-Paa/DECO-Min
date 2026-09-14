using UnityEngine;
using TMPro;

/// <summary>
/// シール数を「× 9999」の形式で表示するUI。
/// </summary>
public class SealCountUI : UIPlayerBase
{
    private TextMeshProUGUI _sealCountText;

    protected override void Start()
    {
        base.Start();

        _sealCountText = GetComponent<TextMeshProUGUI>();

        if (_sealCountText == null)
        {
            Debug.LogWarning($"{nameof(SealCountUI)} : TextMeshProUGUI が見つかりません。同じGameObjectに付けてください。");
        }
    }

    private void Update()
    {
        if (_playerStatus == null || _sealCountText == null)
        {
            return;
        }

        _sealCountText.text = "× " + _playerStatus.CurrentSealCount.ToString();
    }
}