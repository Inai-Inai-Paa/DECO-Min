using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SealCountUI : UIPlayerBase
{
    private TextMeshProUGUI _sealCountText;
    private UITextCharacterAnimation _characterAnimation;

    private int _previousSealCount;
    private bool _hasPreviousCount;

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

#if UNITY_EDITOR
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                _playerStatus.CurrentSealCount = Mathf.Min(
                    9999,
                    _playerStatus.CurrentSealCount + 9
                );
            }

            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                _playerStatus.CurrentSealCount = Mathf.Max(
                    0,
                    _playerStatus.CurrentSealCount - 9
                );
            }
        }
#endif

        int sealCount = _playerStatus.CurrentSealCount;
        int displayCount = Mathf.Clamp(
            sealCount,
            0,
            9999
        );

        string displayText = "×" + displayCount.ToString("D4");

        if (_sealCountText.text != displayText)
        {
            _sealCountText.text = displayText;
        }

        if (_hasPreviousCount &&
            _previousSealCount != sealCount &&
            _characterAnimation != null)
        {
            _characterAnimation.Play(1);
        }

        _previousSealCount = sealCount;
        _hasPreviousCount = true;
    }
}