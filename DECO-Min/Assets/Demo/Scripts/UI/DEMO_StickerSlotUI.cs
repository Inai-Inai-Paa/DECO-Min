using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single sticker slot in the Sticker Book grid UI.
/// Shows icon color, count, name and rarity border — all in English.
/// </summary>
public class DEMO_StickerSlotUI : MonoBehaviour
{
    [SerializeField] private Image           iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image           rarityBorder;
    [SerializeField] private GameObject      highlightOverlay;
    [SerializeField] private Button          button;

    public DEMO_StickerData StickerData { get; private set; }

    private Action<DEMO_StickerData> onClick;

    public void Setup(DEMO_StickerData data, int count, Action<DEMO_StickerData> callback)
    {
        StickerData = data;
        onClick     = callback;

        if (iconImage   != null)
        {
            iconImage.color = data.primaryColor;
            if (data.icon) iconImage.sprite = data.icon;
        }
        if (countText   != null) countText.text = $"x{count}";
        if (nameText    != null) nameText.text  = data.stickerName;
        if (rarityBorder != null) rarityBorder.color = data.RarityColor;

        SetHighlight(false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(StickerData));
        }
    }

    public void SetHighlight(bool on)
    {
        if (highlightOverlay != null) highlightOverlay.SetActive(on);

        if (button != null)
        {
            var c = button.colors;
            c.normalColor = on ? new Color(1f, 0.95f, 0.55f) : Color.white;
            button.colors = c;
        }
    }
}
