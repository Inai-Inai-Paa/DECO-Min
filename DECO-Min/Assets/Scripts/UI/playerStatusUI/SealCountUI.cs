using UnityEngine;
using TMPro;

/// <summary>
/// シール数を「Current / Max」で表示するUI。
/// </summary>
public class SealCountUI : UIPlayerBase
{
    [Header("Seal Count UI")]

    [SerializeField]
    private TextMeshProUGUI _sealCountText;
    protected override void Awake()
    {
        base.Awake();

        _sealCountText = GetComponent<TextMeshProUGUI>();

        if (_sealCountText == null)
        {
            Debug.LogWarning($"{nameof(SealCountUI)} : TextMeshProUGUI が見つかりません。同じGameObjectに付けてください。");
        }
    }

    private void Update()
    {
        if (_playerStatus == null)
        {
            return;
        }

        if (_sealCountText == null)
        {
            return;
        }

        _sealCountText.text =
            _playerStatus.CurrentSealCount.ToString()
            + " / "
            + _playerStatus.TotalSealCount.ToString();
    }
}