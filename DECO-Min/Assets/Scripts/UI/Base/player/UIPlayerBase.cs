using System.Reflection;
using UnityEngine;

/// <summary>
/// プレイヤーUI用の基底クラス。
/// Player.cs / Character.cs を変更せずに、Playerが持っているCharacterStatusからPlayerStatusを取得する。
/// </summary>
public class UIPlayerBase : UIBase
{
    [Header("Player UI Base")]

    [SerializeField]
    protected Player _player;

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
            return;
        }

        _playerStatus = GetPlayerStatusFromPlayer(_player);

        if (_playerStatus == null)
        {
            Debug.LogWarning($"{nameof(UIPlayerBase)} : PlayerStatus を取得できません。PlayerのCharacterStatusにPlayerStatusが入っているか確認して。");
        }
    }

    protected override void Start()
    {
        Show();
    }

    private PlayerStatus GetPlayerStatusFromPlayer(Player player)
    {
        if (player == null)
        {
            return null;
        }

        FieldInfo fieldInfo = typeof(Character).GetField(
            "characterStatus",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (fieldInfo == null)
        {
            Debug.LogWarning($"{nameof(UIPlayerBase)} : Character.characterStatus が見つかりません。");
            return null;
        }

        CharacterStatus status = fieldInfo.GetValue(player) as CharacterStatus;

        return status as PlayerStatus;
    }
}