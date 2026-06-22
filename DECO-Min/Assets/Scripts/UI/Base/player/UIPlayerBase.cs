using UnityEngine;

/// <summary>
/// プレイヤーUI用の基底クラス。
/// </summary>
public class UIPlayerBase : UIBase
{
    [Header("Player UI Base")]

    [SerializeField]
    protected Player _player;

    [SerializeField]
    protected PlayerStatus _playerStatus;

    protected override void Awake()
    {
        base.Awake();

        if (_player == null)
        {
            _player = FindAnyObjectByType<Player>();
        }

        if (_player == null)
        {
            Debug.LogWarning($"{nameof(UIPlayerBase)} : Player が見つかりません。");
        }
    }

    protected override void Start()
    {
        Show();

        if (_playerStatus == null)
        {
            Debug.LogWarning($"{nameof(UIPlayerBase)} : PlayerStatus が設定されていません。InspectorにPlayerStatusを入れて。");
        }
    }

    protected bool IsValidPlayerStatus()
    {
        return _playerStatus != null;
    }
}