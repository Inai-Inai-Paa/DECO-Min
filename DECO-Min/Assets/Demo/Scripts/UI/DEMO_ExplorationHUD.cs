using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// In-exploration HUD: timer, selected sticker, sticker count, controls hint,
/// proximity hint ("Press E to peel!"), and peel progress.
/// All text is in English.
/// </summary>
public class DEMO_ExplorationHUD : MonoBehaviour
{
    // ─── Inspector ─────────────────────────────────────────────────────────────
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Slider          timerSlider;
    [SerializeField] private Color           normalColor = Color.white;
    [SerializeField] private Color           urgentColor = new(1f, 0.3f, 0.3f);
    [SerializeField] private float           urgentThreshold = 60f;

    [Header("Selected Sticker")]
    [SerializeField] private Image           stickerIcon;
    [SerializeField] private TextMeshProUGUI stickerNameText;
    [SerializeField] private TextMeshProUGUI stickerCountText;
    [SerializeField] private Image           rarityBorder;
    [SerializeField] private TextMeshProUGUI totalRemainingText;

    [Header("Controls Hint")]
    [SerializeField] private TextMeshProUGUI controlsText;

    [Header("Proximity Hint (shown near stickered objects)")]
    [SerializeField] private GameObject      peelHintRoot;
    [SerializeField] private TextMeshProUGUI peelHintText;
    [SerializeField] private float           peelDetectRadius = 2.5f;

    [Header("Peel Progress")]
    [SerializeField] private GameObject      peelProgressRoot;
    [SerializeField] private Slider          peelProgressSlider;
    [SerializeField] private TextMeshProUGUI peelProgressLabel;

    [Header("Return Button")]
    [SerializeField] private Button returnButton;

    // ─── Private ──────────────────────────────────────────────────────────────
    private DEMO_StickerBook     sb;
    private DEMO_GameManager     gm;
    private DEMO_PlayerController player;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        sb     = DEMO_StickerBook.Instance ?? FindFirstObjectByType<DEMO_StickerBook>();
        gm     = DEMO_GameManager.Instance;
        player = FindFirstObjectByType<DEMO_PlayerController>();

        if (sb != null)
        {
            sb.OnSelectedStickerChanged.AddListener(RefreshSticker);
            sb.OnInventoryChanged.AddListener((_, _) =>
            {
                RefreshSticker(sb.SelectedSticker);
                RefreshTotal();
            });
        }

        if (returnButton != null)
            returnButton.onClick.AddListener(() => gm?.EndExploration());

        if (controlsText != null)
            controlsText.text =
                "Left Click : Throw sticker\n" +
                "Right Click : Aim mode\n" +
                "E : Peel stickered object\n" +
                "Scroll / 1-9 : Switch sticker\n" +
                "Shift : Sprint";

        if (peelHintRoot  != null) peelHintRoot.SetActive(false);
        if (peelProgressRoot != null) peelProgressRoot.SetActive(false);

        RefreshSticker(sb?.SelectedSticker);
        RefreshTotal();
    }

    private void Update()
    {
        RefreshTimer();
        CheckPeelProximity();
    }

    // ─── Timer ────────────────────────────────────────────────────────────────
    private void RefreshTimer()
    {
        if (gm == null) return;
        float t   = gm.ExplorationTimeRemaining;
        float max = gm.ExplorationTimeMax;

        if (timerText != null)
        {
            timerText.text  = $"{Mathf.FloorToInt(t / 60f):00}:{Mathf.FloorToInt(t % 60f):00}";
            timerText.color = t <= urgentThreshold ? urgentColor : normalColor;
        }
        if (timerSlider != null && max > 0f)
            timerSlider.value = t / max;
    }

    // ─── Selected sticker display ─────────────────────────────────────────────
    private void RefreshSticker(DEMO_StickerData data)
    {
        bool has = data != null && sb != null && sb.GetStickerCount(data) > 0;

        if (stickerNameText  != null) stickerNameText.text  = has ? data.stickerName : "No sticker";
        if (stickerCountText != null) stickerCountText.text = has ? $"x{sb.GetStickerCount(data)}" : "x0";
        if (rarityBorder     != null && has) rarityBorder.color = data.RarityColor;

        if (stickerIcon != null)
        {
            stickerIcon.gameObject.SetActive(has);
            if (has) { stickerIcon.color = data.primaryColor; }
        }
    }

    private void RefreshTotal()
    {
        if (totalRemainingText != null && sb != null)
            totalRemainingText.text = $"Total: {sb.TotalCount}";
    }

    // ─── Peel proximity hint ──────────────────────────────────────────────────
    private void CheckPeelProximity()
    {
        if (peelHintRoot == null || player == null) return;

        bool near = false;
        var hits = Physics.OverlapSphere(player.transform.position, peelDetectRadius);
        foreach (var h in hits)
        {
            if (h.GetComponent<DEMO_StickerizedObject>() != null) { near = true; break; }
        }

        peelHintRoot.SetActive(near);
        if (near && peelHintText != null)
            peelHintText.text = "Press  E  to peel!";
    }

    // ─── Peel progress (called by UIManager) ─────────────────────────────────
    public void ShowPeelProgress(float t, string targetName)
    {
        if (peelProgressRoot == null) return;

        if (t < 0f)
        {
            peelProgressRoot.SetActive(false);
            return;
        }
        peelProgressRoot.SetActive(true);
        if (peelProgressSlider != null) peelProgressSlider.value = t;
        if (peelProgressLabel  != null) peelProgressLabel.text   = $"Peeling \"{targetName}\"...";
    }
}
