using UnityEngine;

/// <summary>
/// シーン起動時に必要なシングルトンが存在しない場合に自動生成するブートストラッパー。
///
/// 使い方：
///   シーン内の任意の GameObject に本スクリプトをアタッチするだけ。
///   GameManager / StickerBook / UIManager が存在しない場合のみ生成する。
///   本番ではシーンに直接 Prefab を配置する運用にしても構わない。
/// </summary>
public class DEMO_SceneBootstrapper : MonoBehaviour
{
    [Header("自動生成設定")]
    [Tooltip("チェックした場合、シーンに GameManager が無ければ自動生成します")]
    [SerializeField] private bool autoCreateGameManager = true;

    [Tooltip("チェックした場合、シーンに StickerBook が無ければ自動生成します")]
    [SerializeField] private bool autoCreateStickerBook = true;

    [Tooltip("デバッグ用の初期シール（StickerBook 自動生成時のみ参照）")]
    [SerializeField] private DEMO_StickerData[] debugStartingStickers;
    [SerializeField] private int           debugStartingCount = 3;

    // ──────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        EnsureGameManager();
        EnsureStickerBook();
    }

    // ─── GameManager ──────────────────────────────────────────────────────────
    private void EnsureGameManager()
    {
        if (!autoCreateGameManager) return;
        if (FindFirstObjectByType<DEMO_GameManager>() != null) return;

        var go = new GameObject("[GameManager]");
        go.AddComponent<DEMO_GameManager>();
        Debug.Log("[SceneBootstrapper] GameManager を自動生成しました");
    }

    // ─── StickerBook ─────────────────────────────────────────────────────────
    private void EnsureStickerBook()
    {
        if (!autoCreateStickerBook) return;
        if (FindFirstObjectByType<DEMO_StickerBook>() != null) return;

        var go = new GameObject("[StickerBook]");
        go.AddComponent<DEMO_StickerBook>();
        Debug.Log("[SceneBootstrapper] StickerBook を自動生成しました");
    }
}
