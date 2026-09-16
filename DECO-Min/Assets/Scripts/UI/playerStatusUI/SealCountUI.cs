using TMPro;
using UnityEngine;

/// <summary>
/// シール数を「× 9999」の形式で表示するUI。
/// </summary>
public class SealCountUI : UIPlayerBase
{
    private TextMeshProUGUI _sealCountText;
    private UITextCharacterAnimation _characterAnimation;

    private int _previousSealCount = -1;

    protected override void Start()
    {
        base.Start();

        _sealCountText = GetComponent<TextMeshProUGUI>();
        _characterAnimation = GetComponent<UITextCharacterAnimation>();

        if (_sealCountText == null)
        {
            Debug.LogWarning($"{nameof(SealCountUI)} : TextMeshProUGUI が見つかりません。同じGameObjectに付けてください。");
        }

        if (_characterAnimation == null)
        {
            Debug.LogWarning($"{nameof(SealCountUI)} : UITextCharacterAnimation が見つかりません。同じGameObjectに付けてください。");
        }
    }

    private void Update()
    {
        if (_playerStatus == null || _sealCountText == null)
        {
            return;
        }

        int sealCount = _playerStatus.CurrentSealCount;

        if (_previousSealCount == sealCount)
        {
            return;
        }

        _previousSealCount = sealCount;

        _sealCountText.text = "× " + sealCount.ToString();

        if (_characterAnimation != null)
        {
            _characterAnimation.Play(2);
        }
    }
}