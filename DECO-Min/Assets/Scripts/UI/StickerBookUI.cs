using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sticker Book screen — shows the player's sticker inventory before each exploration.
/// All text in English.
/// </summary>
public class StickerBookUI : MonoBehaviour
{
    // ─── Inspector ─────────────────────────────────────────────────────────────
    [Header("Grid")]
    [SerializeField] private Transform  stickerGridParent;
    [SerializeField] private GameObject stickerSlotPrefab;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI kindsText;

    [Header("Detail Panel")]
    [SerializeField] private GameObject      detailPanel;
    [SerializeField] private Image           detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailRarity;
    [SerializeField] private TextMeshProUGUI detailDesc;
    [SerializeField] private TextMeshProUGUI detailPower;
    [SerializeField] private TextMeshProUGUI detailCount;
    [SerializeField] private Image           detailBorder;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonLabel;

    // ─── Private ──────────────────────────────────────────────────────────────
    private StickerBook           sb;
    private List<StickerSlotUI>   slots = new();

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        sb = StickerBook.Instance ?? FindFirstObjectByType<StickerBook>();
    }

    private void Start()
    {
        if (sb != null)
            sb.OnInventoryChanged.AddListener((_, _) => Refresh());

        if (startButton != null)
            startButton.onClick.AddListener(() => UIManager.Instance?.OnStartExplorationClicked());

        if (startButtonLabel != null)
            startButtonLabel.text = "Start Exploration";

        if (GameManager.Instance != null)
            GameManager.Instance.OnDayChanged.AddListener(d => UpdateDay(d));

        if (detailPanel != null) detailPanel.SetActive(false);

        UpdateDay(GameManager.Instance?.CurrentDay ?? 1);
        Refresh();
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public void Refresh()
    {
        if (sb == null) return;

        // Clear existing slots
        foreach (var s in slots) if (s) Destroy(s.gameObject);
        slots.Clear();

        // Rebuild grid
        foreach (var entry in sb.GetAllEntries())
        {
            if (entry.Count <= 0 || stickerSlotPrefab == null || stickerGridParent == null)
                continue;

            var go   = Instantiate(stickerSlotPrefab, stickerGridParent);
            var slot = go.GetComponent<StickerSlotUI>();
            if (slot != null)
            {
                slot.Setup(entry.Data, entry.Count, ShowDetail);
                slots.Add(slot);
            }
        }

        // Header
        if (totalText != null) totalText.text = $"Total: {sb.TotalCount} sticker(s)";
        if (kindsText != null) kindsText.text  = $"{sb.KindCount} type(s)";

        // Start button state
        if (startButton != null) startButton.interactable = sb.HasAnySticker();
        if (startButton == null && !sb.HasAnySticker())
            Debug.LogWarning("[StickerBookUI] No stickers in book — cannot start exploration.");
    }

    // ─── Detail panel ─────────────────────────────────────────────────────────
    private void ShowDetail(StickerData data)
    {
        if (detailPanel == null || data == null) return;
        detailPanel.SetActive(true);

        if (detailIcon   != null) { detailIcon.color = data.primaryColor; if (data.icon) detailIcon.sprite = data.icon; }
        if (detailName   != null) detailName.text   = data.stickerName;
        if (detailRarity != null) detailRarity.text = data.RarityLabel;
        if (detailDesc   != null) detailDesc.text   = data.description;
        if (detailPower  != null) detailPower.text  = $"Sticker Power: {data.stickerPower}";
        if (detailCount  != null) detailCount.text  = $"In book: {sb.GetStickerCount(data)}";
        if (detailBorder != null) detailBorder.color = data.RarityColor;

        // Highlight slot
        foreach (var s in slots)
            s.SetHighlight(s.StickerData == data);
    }

    private void UpdateDay(int day)
    {
        if (dayText != null) dayText.text = $"Day  {day}";
    }
}
