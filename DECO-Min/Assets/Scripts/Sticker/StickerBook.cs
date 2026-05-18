using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// プレイヤーのシール帳（在庫）を管理するシングルトン。
/// ピクミンで言うオリマーに相当し、持てるシールの種類・枚数を管理する。
/// </summary>
public class StickerBook : MonoBehaviour
{
    public static StickerBook Instance { get; private set; }

    // ─── インスペクター設定 ────────────────────────────────────────────────────
    [Header("シール帳設定")]
    [SerializeField] private int maxStickerKinds = 20;  // 登録できるシールの種類上限
    [SerializeField] private int maxPerSticker   = 99;  // 同一シールの最大所持枚数

    [Header("初期シール（デバッグ・スタート用）")]
    [SerializeField] private List<StickerData> startingStickers = new();
    [SerializeField] private int               startingCount    = 3;

    // ─── イベント ─────────────────────────────────────────────────────────────
    /// <summary>在庫が変化したとき（変化したシール, 新しい枚数）</summary>
    [HideInInspector] public UnityEvent<StickerData, int> OnInventoryChanged      = new();
    /// <summary>選択中シールが変わったとき</summary>
    [HideInInspector] public UnityEvent<StickerData>      OnSelectedStickerChanged = new();

    // ─── プライベートフィールド ────────────────────────────────────────────────
    // OrderedDictionary の代わりに List<StickerEntry> でキー順を維持する
    private readonly List<StickerEntry> inventory = new();

    // ─── パブリックプロパティ ─────────────────────────────────────────────────
    public StickerData SelectedSticker { get; private set; }
    public int         MaxStickerKinds => maxStickerKinds;
    public int         KindCount       => inventory.Count(e => e.Count > 0);

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        foreach (var sticker in startingStickers)
            AddSticker(sticker, startingCount);

        if (startingStickers.Count > 0)
            SelectSticker(startingStickers[0]);
    }

    // ─── 公開 API ─────────────────────────────────────────────────────────────

    /// <summary>シールを指定枚数追加する。上限を超えた分は切り捨て。</summary>
    public bool AddSticker(StickerData sticker, int count = 1)
    {
        if (sticker == null || count <= 0) return false;

        var entry = inventory.Find(e => e.Data == sticker);
        if (entry == null)
        {
            if (inventory.Count >= maxStickerKinds)
            {
                Debug.LogWarning($"[StickerBook] シール種類上限（{maxStickerKinds}）に達しているため {sticker.stickerName} を追加できません");
                return false;
            }
            entry = new StickerEntry(sticker, 0);
            inventory.Add(entry);
        }

        int before  = entry.Count;
        entry.Count = Mathf.Min(entry.Count + count, maxPerSticker);
        int added   = entry.Count - before;

        OnInventoryChanged.Invoke(sticker, entry.Count);
        Debug.Log($"[StickerBook] 追加: {sticker.stickerName} +{added} (合計 {entry.Count}枚)");
        return true;
    }

    /// <summary>シールを指定枚数消費する（投げる際など）。不足の場合は false を返す。</summary>
    public bool ConsumeSticker(StickerData sticker, int count = 1)
    {
        if (sticker == null) return false;

        var entry = inventory.Find(e => e.Data == sticker);
        if (entry == null || entry.Count < count) return false;

        entry.Count -= count;
        OnInventoryChanged.Invoke(sticker, entry.Count);

        // 在庫が尽きたら次のシールに自動切替
        if (entry.Count <= 0 && SelectedSticker == sticker)
            AutoSelectNext();

        return true;
    }

    /// <summary>選択中シールを切り替える。</summary>
    public void SelectSticker(StickerData sticker)
    {
        if (sticker == null) return;
        if (!inventory.Any(e => e.Data == sticker && e.Count > 0)) return;

        SelectedSticker = sticker;
        OnSelectedStickerChanged.Invoke(sticker);
    }

    /// <summary>リスト上のインデックスでシールを選択する（UIの数字キー対応）。</summary>
    public void SelectStickerByIndex(int index)
    {
        var available = inventory.Where(e => e.Count > 0).ToList();
        if (index >= 0 && index < available.Count)
            SelectSticker(available[index].Data);
    }

    /// <summary>現在選択中シールの次のシールを選択する（ホイール上）。</summary>
    public void SelectNext()
    {
        var available = inventory.Where(e => e.Count > 0).ToList();
        if (available.Count == 0) return;

        int cur = available.FindIndex(e => e.Data == SelectedSticker);
        int next = (cur + 1) % available.Count;
        SelectSticker(available[next].Data);
    }

    /// <summary>現在選択中シールの前のシールを選択する（ホイール下）。</summary>
    public void SelectPrevious()
    {
        var available = inventory.Where(e => e.Count > 0).ToList();
        if (available.Count == 0) return;

        int cur  = available.FindIndex(e => e.Data == SelectedSticker);
        int prev = (cur - 1 + available.Count) % available.Count;
        SelectSticker(available[prev].Data);
    }

    /// <summary>指定シールの所持枚数を返す。</summary>
    public int GetStickerCount(StickerData sticker)
    {
        if (sticker == null) return 0;
        return inventory.Find(e => e.Data == sticker)?.Count ?? 0;
    }

    /// <summary>在庫一覧を読み取り専用で返す。</summary>
    public IReadOnlyList<StickerEntry> GetAllEntries() => inventory;

    /// <summary>投げられるシールが1枚でもあるか。</summary>
    public bool HasAnySticker() => inventory.Any(e => e.Count > 0);

    /// <summary>全シールの合計枚数。</summary>
    public int TotalCount => inventory.Sum(e => e.Count);

    // ─── プライベートヘルパー ─────────────────────────────────────────────────
    private void AutoSelectNext()
    {
        var next = inventory.FirstOrDefault(e => e.Count > 0);
        if (next != null)
        {
            SelectedSticker = next.Data;
            OnSelectedStickerChanged.Invoke(next.Data);
        }
        else
        {
            SelectedSticker = null;
            OnSelectedStickerChanged.Invoke(null);
        }
    }
}

/// <summary>シール帳の1エントリー（種類 + 枚数）。</summary>
[System.Serializable]
public class StickerEntry
{
    public StickerData Data;
    public int         Count;

    public StickerEntry(StickerData data, int count)
    {
        Data  = data;
        Count = count;
    }
}
