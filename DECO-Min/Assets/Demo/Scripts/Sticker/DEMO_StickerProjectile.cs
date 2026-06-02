using UnityEngine;

/// <summary>
/// Sticker projectile — flight, collision, surface sticking.
///
/// Uses StickerThrower.CalcLaunchVelocity so the ball follows EXACTLY
/// the same arc the player saw in the preview.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DEMO_StickerProjectile : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 380f;

    private DEMO_StickerData stickerData;
    private Rigidbody   rb;
    private Collider    col;
    private bool        hasStuck;

    // Surface-distance threshold for the proximity fallback
    private const float ProxSurface = 0.4f;

    private void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // ── Initialize & launch ──────────────────────────────────────────────────
    /// <summary>
    /// Call immediately after Instantiate.
    /// origin = exact spawn position (same as preview arc start).
    /// target = world-space aim point.
    /// </summary>
    public void Initialize(DEMO_StickerData data, Vector3 origin, Vector3 target)
    {
        stickerData    = data;
        rb.isKinematic = false;

        // Snap to origin in case of floating-point drift
        transform.position = origin;

        ApplyColor(data.primaryColor, glow: true);

        // Use the SAME velocity formula as the preview arc
        rb.linearVelocity = DEMO_StickerThrower.CalcLaunchVelocity(
            origin, target, data.throwForce, data.arcHeight);

        Debug.Log($"[Projectile] Launched '{data.stickerName}'  vel={rb.linearVelocity}");
    }

    // ── Spin + proximity fallback ────────────────────────────────────────────
    private void Update()
    {
        if (hasStuck) return;

        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

        // Fallback: measure surface distance to every FieldObject
        foreach (var fo in FindObjectsByType<DEMO_FieldObject>(FindObjectsSortMode.None))
        {
            if (fo.IsStickerized) continue;
            var foCol = fo.GetComponent<Collider>();
            if (foCol == null) continue;

            Vector3 closest  = foCol.ClosestPoint(transform.position);
            float   surfDist = Vector3.Distance(transform.position, closest);

            if (surfDist < ProxSurface)
            {
                Debug.Log($"[Projectile] Proximity hit '{fo.DisplayName}'  dist={surfDist:F3}");
                TriggerStick(fo.transform, closest,
                    (transform.position - fo.transform.position).normalized, fo);
                return;
            }
        }
    }

    // ── Primary collision path ────────────────────────────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        if (hasStuck) return;
        var contact = collision.contacts[0];

        if (collision.gameObject.TryGetComponent<DEMO_FieldObject>(out var fo))
        {
            Debug.Log($"[Projectile] OnCollision hit '{fo.DisplayName}'");
            TriggerStick(fo.transform, contact.point, contact.normal, fo);
        }
        else
        {
            // Missed — stick to ground/wall and add DroppedSticker so player can retrieve it
            Debug.Log($"[Projectile] Missed '{collision.gameObject.name}' — dropped sticker placed.");
            hasStuck = true;
            StopPhysics();
            StickToSurface(null, contact.point, contact.normal, missed: true);

            // DroppedSticker handles lifetime and E-key retrieval
            var dropped = gameObject.AddComponent<DEMO_DroppedSticker>();
            dropped.Setup(stickerData);
        }
    }

    private void TriggerStick(Transform parent, Vector3 point,
                               Vector3 normal, DEMO_FieldObject fo)
    {
        hasStuck = true;
        StopPhysics();
        StickToSurface(parent, point, normal);
        fo.OnStickerApplied(stickerData, gameObject);
    }

    // ── Stop Rigidbody ───────────────────────────────────────────────────────
    private void StopPhysics()
    {
        rb.isKinematic     = true;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        if (col != null) col.enabled = false;
    }

    // ── Snap & flatten into a sticker disc ───────────────────────────────────
    private void StickToSurface(Transform parent, Vector3 point,
                                 Vector3 normal, bool missed = false)
    {
        if (parent != null) transform.SetParent(parent);

        Vector3 n = normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up;
        transform.position   = point + n * 0.022f;
        transform.rotation   = Quaternion.LookRotation(-n);
        transform.localScale = new Vector3(0.5f, 0.5f, 0.05f);

        Color c = missed
            ? Color.Lerp(stickerData?.primaryColor ?? Color.white, Color.gray, 0.55f)
            : (stickerData?.primaryColor ?? Color.white);

        ApplyColor(c, glow: false);
    }

    // ── Material helper ──────────────────────────────────────────────────────
    private void ApplyColor(Color c, bool glow)
    {
        if (!TryGetComponent<Renderer>(out var rend)) return;
        var mat = rend.material;
        if (mat == null) return;

        mat.color = c;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_EmissionColor"))
        {
            if (glow)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 2.5f);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
