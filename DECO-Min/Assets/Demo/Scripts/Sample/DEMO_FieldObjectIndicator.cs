using System.Collections;
using UnityEngine;

/// <summary>
/// Attaches floating progress text and color feedback to a FieldObject.
/// Added automatically by SampleBootstrap — no manual setup needed.
///
/// Feedback summary:
///   • Floating text above object: "0 / 5" → progress fraction
///   • Object tints green as stickers accumulate
///   • White flash on each sticker hit
///   • Yellow glow + "STICKERED!  Press E" when threshold reached
/// </summary>
[RequireComponent(typeof(DEMO_FieldObject))]
public class DEMO_FieldObjectIndicator : MonoBehaviour
{
    // ─── Inspector ─────────────────────────────────────────────────────────────
    [Header("Text Offset")]
    [SerializeField] private Vector3 textOffset = new(0f, 1.8f, 0f);

    [Header("Colors")]
    [SerializeField] private Color baseColor      = new(0.72f, 0.55f, 0.36f);  // wood-brown
    [SerializeField] private Color fullColor      = new(0.30f, 1.00f, 0.40f);  // bright green
    [SerializeField] private Color stickerizedColor = new(1.00f, 0.85f, 0.10f); // gold

    // ─── Private ──────────────────────────────────────────────────────────────
    private DEMO_FieldObject  fieldObject;
    private Renderer     objectRenderer;
    private Material     instanceMat;     // per-instance material copy
    private TextMesh     label;
    private Coroutine    flashRoutine;
    private bool         isStickered;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        fieldObject    = GetComponent<DEMO_FieldObject>();
        objectRenderer = GetComponent<Renderer>();

        // Make a per-instance material so we can tint independently
        if (objectRenderer != null && objectRenderer.sharedMaterial != null)
        {
            instanceMat = new Material(objectRenderer.sharedMaterial);
            SetMatColor(instanceMat, baseColor);
            objectRenderer.material = instanceMat;
        }

        BuildLabel();
    }

    private void Start()
    {
        // Subscribe to FieldObject events
        fieldObject.OnStickerPowerChanged.AddListener(OnProgress);
        fieldObject.OnStickerized.AddListener(OnStickered);

        RefreshLabel(0, fieldObject.StickerThreshold);
    }

    // ─── Label construction ───────────────────────────────────────────────────
    private void BuildLabel()
    {
        var go = new GameObject("ProgressLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = textOffset;

        label                = go.AddComponent<TextMesh>();
        label.fontSize       = 52;
        label.characterSize  = 0.045f;
        label.anchor         = TextAnchor.MiddleCenter;
        label.alignment      = TextAlignment.Center;
        label.color          = Color.white;

        // Always face the camera
        go.AddComponent<DEMO_FaceCamera>();
    }

    // ─── Event callbacks ──────────────────────────────────────────────────────
    private void OnProgress(int current, int max)
    {
        RefreshLabel(current, max);
        TintByProgress((float)current / max);

        // Brief white flash
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlash());
    }

    private void OnStickered()
    {
        isStickered = true;

        if (label != null)
        {
            label.text  = "STICKERED!\n[Press E]";
            label.color = stickerizedColor;

            // Scale up label for visibility
            label.transform.localScale = Vector3.one * 1.3f;
        }

        if (instanceMat != null)
            SetMatColor(instanceMat, stickerizedColor);

        // Gentle bounce animation
        StartCoroutine(BounceScale());
    }

    // ─── Visual helpers ───────────────────────────────────────────────────────
    private void RefreshLabel(int current, int max)
    {
        if (label == null) return;
        label.text  = $"{current} / {max}";
        label.color = Color.Lerp(Color.white, new Color(0.4f, 1f, 0.4f),
                                 max > 0 ? (float)current / max : 0f);
    }

    private void TintByProgress(float t)
    {
        if (instanceMat == null) return;
        SetMatColor(instanceMat, Color.Lerp(baseColor, fullColor, t * 0.6f));
    }

    // White flash on sticker hit
    private IEnumerator HitFlash()
    {
        if (instanceMat == null) yield break;
        Color before = instanceMat.color;
        SetMatColor(instanceMat, Color.white);
        yield return new WaitForSeconds(0.08f);
        if (!isStickered) SetMatColor(instanceMat, before);
    }

    // Scale bounce when stickered
    private IEnumerator BounceScale()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    // ─── Material color helper (URP + Standard shader safe) ──────────────────
    private static void SetMatColor(Material mat, Color color)
    {
        mat.color = color;
        // URP uses _BaseColor; set both for compatibility
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
    }
}
