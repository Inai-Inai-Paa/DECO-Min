using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages which UI panel is visible based on the current GamePhase.
/// All displayed text is in English.
/// </summary>
public class DEMO_UIManager : MonoBehaviour
{
    public static DEMO_UIManager Instance { get; private set; }

    // ─── Inspector ─────────────────────────────────────────────────────────────
    [Header("Main Panels")]
    [SerializeField] private GameObject stickerBookPanel;
    [SerializeField] private GameObject explorationPanel;
    [SerializeField] private GameObject returnPanel;

    [Header("Sub-components")]
    [SerializeField] private DEMO_StickerBookUI  stickerBookUI;
    [SerializeField] private DEMO_ExplorationHUD explorationHUD;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (DEMO_GameManager.Instance != null)
            DEMO_GameManager.Instance.OnPhaseChanged.AddListener(HandlePhaseChange);

        HandlePhaseChange(DEMO_GameManager.Instance?.CurrentPhase ?? GamePhase.StickerBook);
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
    public void OnStartExplorationClicked() => DEMO_GameManager.Instance?.StartExploration();
    public void OnEndExplorationClicked()   => DEMO_GameManager.Instance?.EndExploration();

    public void SelectStickerByIndex(int index) =>
        DEMO_StickerBook.Instance?.SelectStickerByIndex(index);

    // ─── Peel progress (forwarded to ExplorationHUD) ─────────────────────────
    public void NotifyPeelProgress(float progress, string targetName)
        => explorationHUD?.ShowPeelProgress(progress, targetName);

    // ─── Utility ─────────────────────────────────────────────────────────────
    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
