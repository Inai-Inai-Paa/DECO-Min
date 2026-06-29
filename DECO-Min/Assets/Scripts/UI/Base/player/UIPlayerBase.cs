using System.ComponentModel;
using UnityEngine;

/// <summary>
/// プレイヤーUI用の基底クラス。
/// </summary>
public class UIPlayerBase : UIBase
{
    [Header("Player UI Base")]

    [SerializeField]
    protected Player _player;

    protected PlayerStatus _playerStatus;

    protected override void Start()
    {
        base.Start();

        if (_player == null)
        {
            _player = FindAnyObjectByType<Player>();

        }

        if (_player != null && _playerStatus == null)
        {
            _playerStatus = _player.Status;
        }

        if (_player == null)
        {
            Debug.LogWarning($"{nameof(UIPlayerBase)} : Player が見つかりません。");
        }

        if (_playerStatus == null)
        {
            Debug.LogWarning($"{nameof(UIPlayerBase)} : PlayerStatus が設定されていません。PlayerのStatusを確認してください。");
        }

        Show();
    }

    protected bool IsValidPlayerStatus()
    {
        return _playerStatus != null;
    }
}