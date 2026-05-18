using UnityEngine;

/// <summary>
/// シール1種類の定義データ（ScriptableObject）。
/// Project ウィンドウ右クリック → Create → BonbonDrop → シールデータ で作成できます。
/// </summary>
[CreateAssetMenu(fileName = "NewSticker", menuName = "BonbonDrop/シールデータ")]
public class StickerData : ScriptableObject
{
    // ─── 基本情報 ─────────────────────────────────────────────────────────────
    [Header("基本情報")]
    public string stickerName = "新しいシール";

    [TextArea(2, 4)]
    public string description = "ここにシールの説明を書く";

    [Tooltip("シール帳UIに表示するアイコン画像")]
    public Sprite icon;

    // ─── 3Dプレハブ ───────────────────────────────────────────────────────────
    [Header("3Dプレハブ")]
    [Tooltip("投げたときに飛ぶ物体のプレハブ（StickerProjectile コンポーネント必須）")]
    public GameObject projectilePrefab;

    [Tooltip("オブジェクトに貼り付いた後のデカールプレハブ（任意）")]
    public GameObject decalPrefab;

    // ─── 外観 ─────────────────────────────────────────────────────────────────
    [Header("外観カラー")]
    public Color primaryColor   = Color.white;
    public Color secondaryColor = Color.white;

    // ─── 投てき設定 ───────────────────────────────────────────────────────────
    [Header("投てきパラメータ")]
    [Range(5f, 30f)]
    [Tooltip("投げる速度（大きいほど遠くに飛ぶ）")]
    public float throwForce = 12f;

    [Range(0.05f, 1f)]
    [Tooltip("着弾判定のコライダー半径")]
    public float collisionRadius = 0.2f;

    [Range(0f, 5f)]
    [Tooltip("放物線の山の高さ補正値")]
    public float arcHeight = 1.5f;

    // ─── シール効果 ───────────────────────────────────────────────────────────
    [Header("シール効果")]
    [Range(1, 10)]
    [Tooltip("このシール1枚がオブジェクトのシールカウントに加算する値")]
    public int stickerPower = 1;

    // ─── レアリティ ───────────────────────────────────────────────────────────
    [Header("レアリティ")]
    public StickerRarity rarity = StickerRarity.Common;

    /// <summary>レアリティに対応したカラーを返す。UIのボーダー色などに使用。</summary>
    public Color RarityColor => rarity switch
    {
        StickerRarity.Common   => new Color(0.70f, 0.70f, 0.70f),
        StickerRarity.Uncommon => new Color(0.20f, 0.80f, 0.20f),
        StickerRarity.Rare     => new Color(0.20f, 0.40f, 0.95f),
        StickerRarity.Special  => new Color(1.00f, 0.80f, 0.00f),
        _                      => Color.white
    };

    /// <summary>Returns the rarity as an English display string.</summary>
    public string RarityLabel => rarity switch
    {
        StickerRarity.Common   => "Common",
        StickerRarity.Uncommon => "Uncommon",
        StickerRarity.Rare     => "Rare",
        StickerRarity.Special  => "Special",
        _                      => "Unknown"
    };
}

public enum StickerRarity
{
    Common,    // コモン  ─── 灰色
    Uncommon,  // アンコモン ─ 緑
    Rare,      // レア ──── 青
    Special    // スペシャル ─ 金
}
