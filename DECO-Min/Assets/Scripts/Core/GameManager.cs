using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Game phase manager — singleton, no DontDestroyOnLoad.
/// Single-scene prototype does not need cross-scene persistence.
/// Removing DontDestroyOnLoad eliminates the most common cause of
/// phase-state corruption when entering/re-entering Play mode.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Always win: destroy any older instance, keep this one
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);
        Instance = this;
        // NOTE: DontDestroyOnLoad intentionally removed — single-scene only
        InitializeGame();
    }

    // ── Events ────────────────────────────────────────────────────────────────
    [HideInInspector] public UnityEvent<GamePhase> OnPhaseChanged      = new();
    [HideInInspector] public UnityEvent<int>        OnDayChanged        = new();
    [HideInInspector] public UnityEvent<float>      OnTimeUpdated       = new();

    // ── Properties ────────────────────────────────────────────────────────────
    public GamePhase CurrentPhase             { get; private set; }
    public int        CurrentDay              { get; private set; } = 1;
    public float      ExplorationTimeMax      { get; private set; }
    public float      ExplorationTimeRemaining{ get; private set; }
    public bool       IsExplorationActive     { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Timer")]
    [SerializeField] private float explorationTimeLimitSeconds = 240f;
    [SerializeField] private bool  useTimeLimit                = true;

    [Header("Return phase duration (s)")]
    [SerializeField] private float returnPhaseDuration = 2.5f;

    // ─────────────────────────────────────────────────────────────────────────
    private void InitializeGame()
    {
        ExplorationTimeMax       = explorationTimeLimitSeconds;
        ExplorationTimeRemaining = explorationTimeLimitSeconds;
        CurrentPhase             = GamePhase.StickerBook;
        Debug.Log("[GameManager] Initialized — phase: StickerBook");
    }

    private void Update()
    {
        if (!IsExplorationActive || !useTimeLimit) return;
        ExplorationTimeRemaining -= Time.deltaTime;
        OnTimeUpdated.Invoke(ExplorationTimeRemaining);
        if (ExplorationTimeRemaining <= 0f) { ExplorationTimeRemaining = 0f; EndExploration(); }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void StartExploration()
    {
        if (CurrentPhase != GamePhase.StickerBook)
        {
            Debug.LogWarning($"[GameManager] StartExploration ignored — current phase: {CurrentPhase}");
            return;
        }
        ExplorationTimeRemaining = explorationTimeLimitSeconds;
        IsExplorationActive      = true;
        SetPhase(GamePhase.Exploration);
        Debug.Log($"[GameManager] Exploration STARTED — Day {CurrentDay}");
    }

    public void EndExploration()
    {
        if (!IsExplorationActive) return;
        IsExplorationActive = false;
        SetPhase(GamePhase.Return);
        Debug.Log($"[GameManager] Exploration ended — returning");
        Invoke(nameof(AdvanceToNextDay), returnPhaseDuration);
    }

    private void AdvanceToNextDay()
    {
        CurrentDay++;
        OnDayChanged.Invoke(CurrentDay);
        SetPhase(GamePhase.StickerBook);
        Debug.Log($"[GameManager] Day {CurrentDay} — StickerBook phase");
    }

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged.Invoke(phase);
    }
}

public enum GamePhase { StickerBook, Exploration, Return }
