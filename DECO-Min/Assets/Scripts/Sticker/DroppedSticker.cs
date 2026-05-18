using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to a sticker that missed its target and landed on the ground or a wall.
/// The player can walk up and press E to retrieve it back into their StickerBook.
///
/// Visual feedback:
///   • Pulses in size to catch the player's eye
///   • Glows brighter when the player is within pickup range
///   • Short scale-up + destroy animation on pickup
/// </summary>
public class DroppedSticker : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────────────────
    [SerializeField] private float pickupRadius  = 2.5f;
    [SerializeField] private float lifetime      = 30f;   // auto-despawn after this many seconds
    [SerializeField] private float pulsePeriod   = 1.2f;
    [SerializeField] private float pulseAmount   = 0.18f; // scale oscillation

    // ── State ─────────────────────────────────────────────────────────────────
    private StickerData stickerData;
    private bool        pickedUp   = false;
    private Vector3     baseScale;
    private Renderer    rend;
    private float       spawnTime;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Call immediately after AddComponent to bind the sticker type.</summary>
    public void Setup(StickerData data)
    {
        stickerData = data;
        baseScale   = transform.localScale;
        rend        = GetComponent<Renderer>();
        spawnTime   = Time.time;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (pickedUp) return;

        // Auto-despawn
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Pulse scale
        float t = (Time.time - spawnTime) / pulsePeriod * Mathf.PI * 2f;
        float s = 1f + Mathf.Sin(t) * pulseAmount;
        transform.localScale = baseScale * s;

        // Brightness feedback based on player proximity
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null && rend != null && rend.material != null)
        {
            float dist  = Vector3.Distance(transform.position, player.transform.position);
            float glow  = Mathf.Lerp(0.15f, 2.5f, 1f - Mathf.Clamp01(dist / pickupRadius));
            Color base_ = stickerData != null ? stickerData.primaryColor : Color.white;

            if (rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", base_ * glow);
            }
        }
    }

    // ── Retrieve ──────────────────────────────────────────────────────────────

    /// <summary>Called by PlayerController when E is pressed near this sticker.</summary>
    public void Retrieve()
    {
        if (pickedUp) return;
        pickedUp = true;

        var sb = StickerBook.Instance;
        if (sb != null && stickerData != null)
        {
            sb.AddSticker(stickerData, 1);
            Debug.Log($"[DroppedSticker] Retrieved: {stickerData.stickerName}  " +
                      $"now have x{sb.GetStickerCount(stickerData)}");
        }

        StartCoroutine(PickupAnimation());
    }

    private IEnumerator PickupAnimation()
    {
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float s = 1f + t * 6f;            // scale up quickly
            transform.localScale = baseScale * s;

            if (rend != null && rend.material != null)
            {
                Color c = rend.material.color;
                c.a = 1f - t / 0.25f;          // fade out
                rend.material.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    // ── Accessor ──────────────────────────────────────────────────────────────
    public float PickupRadius => pickupRadius;
}
