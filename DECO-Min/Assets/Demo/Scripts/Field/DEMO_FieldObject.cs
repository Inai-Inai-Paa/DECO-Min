using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A field object that stickers can be applied to.
/// Fix: OnStickerApplied now always increments by 1 (regardless of stickerPower)
///      so every hit reliably counts.
/// </summary>
public class DEMO_FieldObject : MonoBehaviour
{
    [Header("Object Info")]
    [SerializeField] private string objectDisplayName = "Unknown Object";

    [Header("Sticker Settings")]
    [SerializeField] private int stickerThreshold = 5;

    [Header("Reward (set at runtime via RuntimeSetup or Inspector)")]
    [SerializeField] private DEMO_StickerData rewardStickerData;
    [SerializeField] private int         rewardCount = 2;

    [Header("Visuals")]
    [SerializeField] private Material stickerizedMaterial;
    [SerializeField] private GameObject stickerizedEffectPrefab;

    // ── Events ────────────────────────────────────────────────────────────────
    [HideInInspector] public UnityEvent<int, int> OnStickerPowerChanged = new();
    [HideInInspector] public UnityEvent           OnStickerized         = new();

    // ── Properties ────────────────────────────────────────────────────────────
    public int    CurrentStickerPower { get; private set; }
    public int    StickerThreshold    => stickerThreshold;
    public bool   IsStickerized       { get; private set; }
    public string DisplayName         => objectDisplayName;
    public float  Progress            => (float)CurrentStickerPower / stickerThreshold;

    private Renderer objectRenderer;
    private Material normalMaterial;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
            normalMaterial = objectRenderer.sharedMaterial;
    }

    // ── Called by StickerProjectile on collision ──────────────────────────────
    /// <summary>
    /// Always increments by 1 per sticker hit, regardless of stickerPower.
    /// This makes behaviour predictable and prevents "no reaction" bugs.
    /// </summary>
    public void OnStickerApplied(DEMO_StickerData sticker, GameObject visual)
    {
        if (IsStickerized) return;

        CurrentStickerPower += 1;   // always +1 per hit

        Debug.Log($"[FieldObject] '{objectDisplayName}' hit by {sticker?.stickerName ?? "?"}" +
                  $"  {CurrentStickerPower}/{stickerThreshold}");

        OnStickerPowerChanged.Invoke(CurrentStickerPower, stickerThreshold);

        if (CurrentStickerPower >= stickerThreshold)
            TriggerStickerization();
    }

    // ── Stickercize! ─────────────────────────────────────────────────────────
    private void TriggerStickerization()
    {
        if (IsStickerized) return;
        IsStickerized = true;

        Debug.Log($"[FieldObject] '{objectDisplayName}' is now STICKERED!  reward={rewardStickerData?.stickerName ?? "none"}");

        // Visual feedback
        if (objectRenderer != null && stickerizedMaterial != null)
            objectRenderer.material = stickerizedMaterial;

        if (stickerizedEffectPrefab != null)
            Instantiate(stickerizedEffectPrefab, transform.position, Quaternion.identity);

        // Add peel component — keep rewardStickerData reference alive
        var so = gameObject.AddComponent<DEMO_StickerizedObject>();
        so.Initialize(rewardStickerData, rewardCount, objectDisplayName);

        OnStickerized.Invoke();
    }

    // ── Runtime setup (called by SampleBootstrap) ─────────────────────────────
    public void RuntimeSetup(DEMO_StickerData reward, int count = 2)
    {
        rewardStickerData = reward;
        rewardCount       = count;
        Debug.Log($"[FieldObject] '{objectDisplayName}' setup: reward={reward?.stickerName} x{count}  threshold={stickerThreshold}");
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsStickerized ? Color.magenta : Color.cyan;
        var b = GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);
        Gizmos.DrawWireCube(b.center, b.size * 1.05f);
    }
}
