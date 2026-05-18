using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages which UI panel is visible based on the current GamePhase.
/// All displayed text is in English.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ─── Inspector ─────────────────────────────────────────────────────────────
    [Header("Main Panels")]
    [SerializeField] private GameObject stickerBookPanel;
    [SerializeField] private GameObject explorationPanel;
    [SerializeField] private GameObject returnPanel;

    [Header("Sub-components")]
    [SerializeField] private StickerBookUI  stickerBookUI;
    [SerializeField] private ExplorationHUD explorationHUD;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged.AddListener(HandlePhaseChange);

        HandlePhaseChange(GameManager.Instance?.CurrentPhase ?? GamePhase.StickerBook);
    }

    // ─── Phase routing ────────────────────────────────────────────────────────
    private void HandlePhaseChange(GamePhase phase)
    {
        SetActive(stickerBookPanel, phase == GamePhase.StickerBook);
        SetActive(explorationPanel, phase == GamePhase.Exploration);
        SetActive(returnPanel,      phase == GamePhase.Return);

        if (phase == GamePhase.StickerBook) stickerBookUI?.Refresh();
    }

    // ─── Button callbacks ─────────────────────────────────────────────────────
    public void OnStartExplorationClicked() => GameManager.Instance?.StartExploration();
    public void OnEndExplorationClicked()   => GameManager.Instance?.EndExploration();

    public void SelectStickerByIndex(int index) =>
        StickerBook.Instance?.SelectStickerByIndex(index);

    // ─── Peel progress (forwarded to ExplorationHUD) ─────────────────────────
    public void NotifyPeelProgress(float progress, string targetName)
        => explorationHUD?.ShowPeelProgress(progress, targetName);

    // ─── Utility ─────────────────────────────────────────────────────────────
    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
