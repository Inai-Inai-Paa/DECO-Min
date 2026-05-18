using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sticker throwing.
///
/// Input strategy:
///   Primary  : Mouse.current (new Input System)
///   Fallback : T key = instant throw at cursor (bypasses mouse button entirely)
///
/// Logs every step so the cause of "no response" is immediately visible.
/// </summary>
public class StickerThrower : MonoBehaviour
{
    private const float THROW_HEIGHT = 1.4f;

    [SerializeField] private float     maxRange    = 25f;
    [SerializeField] private float     cooldown    = 0.25f;
    [SerializeField] private LayerMask aimMask     = ~0;
    [SerializeField] private int       arcSegments = 32;
    [SerializeField] private float     arcWidth    = 0.10f;

    private Camera      cam;
    private StickerBook sb;

    // Manual edge detection
    private bool  prevHeld  = false;
    private bool  charging  = false;
    private float chargeTime = 0f;

    private Vector3 aimPt;
    private bool    aimOnFO;

    private float lastShot = -99f;

    private LineRenderer arcLine;
    private GameObject   marker;
    private Renderer     markerRend;

    // ── 毎フレームの生InputSystem値を画面に表示（デバッグ用） ─────────────────
    private string dbgLine = "";

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        try { BuildPreview(); }
        catch (System.Exception e)
        { Debug.LogError($"[StickerThrower] BuildPreview exception: {e}"); }
    }

    private void Start()
    {
        cam = Camera.main;
        sb  = StickerBook.Instance ?? FindFirstObjectByType<StickerBook>();
        Debug.Log($"[StickerThrower] Start OK  cam={cam != null}  sb={sb != null}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (cam == null) cam = Camera.main;
        if (sb  == null) sb  = StickerBook.Instance ?? FindFirstObjectByType<StickerBook>();

        StickerSelect();

        // ── Phase check ────────────────────────────────────────────────────
        var gm = GameManager.Instance;
        bool inExploration = gm != null && gm.CurrentPhase == GamePhase.Exploration;
        dbgLine = $"Phase={gm?.CurrentPhase.ToString() ?? "NULL"}  held={Mouse.current?.leftButton.isPressed}  charging={charging}";
        if (!inExploration)
        {
            if (charging) { charging = false; HidePreview(); }
            prevHeld = false;
            return;
        }

        // ── Read mouse ─────────────────────────────────────────────────────
        var mouse = Mouse.current;
        bool held;

        if (mouse != null)
        {
            held = mouse.leftButton.isPressed;
            dbgLine = $"Mouse OK  held={held}  prev={prevHeld}  charging={charging}";
        }
        else
        {
            // Mouse.current is null — Input System device not registered
            held = false;
            dbgLine = "Mouse.current = NULL";
            Debug.LogWarning("[StickerThrower] Mouse.current is null!");
        }

        // Manual edge detection
        bool justDown = held  && !prevHeld;
        bool justUp   = !held &&  prevHeld;
        prevHeld = held;

        // ── T key fallback (throws immediately, no hold needed) ────────────
        bool keyFire = Keyboard.current?.tKey.wasPressedThisFrame == true;
        if (keyFire)
        {
            Debug.Log("[StickerThrower] T-key fallback fire triggered");
            DoAim();
            ReadyToShoot(out string kr);
            Debug.Log($"[StickerThrower] T-key ReadyToShoot={kr}");
            Shoot();
            return;
        }

        // ── Begin charge ───────────────────────────────────────────────────
        if (justDown && !charging)
        {
            bool ready = ReadyToShoot(out string reason);
            Debug.Log($"[StickerThrower] LEFT DOWN  ready={ready}  ({reason})");
            if (ready)
            {
                charging   = true;
                chargeTime = 0f;
                DoAim();
                DrawPreview();
            }
        }

        // ── While held ─────────────────────────────────────────────────────
        if (charging && held)
        {
            chargeTime += Time.deltaTime;
            DoAim();
            DrawPreview();
        }

        // ── Release = fire ─────────────────────────────────────────────────
        if (charging && justUp)
        {
            DoAim();
            charging = false;
            HidePreview();
            Debug.Log($"[StickerThrower] LEFT UP → firing  aimPt={aimPt}  onFO={aimOnFO}");
            Shoot();
        }

        // Safety cancel
        if (charging && !held)
        { charging = false; HidePreview(); }
    }

    // ── IMGUI debug overlay ───────────────────────────────────────────────────
    private void OnGUI()
    {
        if (string.IsNullOrEmpty(dbgLine)) return;
        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, Screen.height - 115, 600, 22), $"[StickerThrower] {dbgLine}");
        GUI.color = Color.white;
    }

    // ── Aim (always produces a valid aimPt) ───────────────────────────────────
    private void DoAim()
    {
        aimOnFO = false;
        // Default: 12 m straight ahead
        aimPt = ThrowOrigin() + transform.forward * 12f;

        if (cam == null) return;

        var mp   = Mouse.current != null ? Mouse.current.position.ReadValue()
                                        : new Vector2(Screen.width * .5f, Screen.height * .5f);
        var ray  = cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
        var hits = Physics.RaycastAll(ray, maxRange, aimMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform root = transform.root;
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(root)) continue;
            aimPt   = h.point;
            aimOnFO = h.collider.GetComponent<FieldObject>() != null;
            break;
        }
    }

    private Vector3 ThrowOrigin() => transform.position + Vector3.up * THROW_HEIGHT;

    // ── Shoot ─────────────────────────────────────────────────────────────────
    private void Shoot()
    {
        var sel = sb?.SelectedSticker;
        if (sel == null)           { Debug.LogWarning("[StickerThrower] No sticker selected"); return; }
        if (!sb.ConsumeSticker(sel)){ Debug.LogWarning("[StickerThrower] ConsumeSticker=false"); return; }

        Vector3 origin = ThrowOrigin();
        var go = CreateSphere(sel);
        go.transform.position = origin;
        go.SetActive(true);

        foreach (var pc in transform.root.GetComponentsInChildren<Collider>(true))
            foreach (var sc in go.GetComponentsInChildren<Collider>(true))
                if (pc && sc) Physics.IgnoreCollision(pc, sc, true);

        if (go.TryGetComponent<StickerProjectile>(out var proj))
            proj.Initialize(sel, origin, aimPt);

        lastShot = Time.time;
        Debug.Log($"[StickerThrower] FIRED '{sel.stickerName}'  {origin}→{aimPt}  left={sb.GetStickerCount(sel)}");
    }

    private bool ReadyToShoot(out string reason)
    {
        if (sb == null)                 { reason = "StickerBook null";    return false; }
        if (!sb.HasAnySticker())        { reason = "book empty";          return false; }
        if (sb.SelectedSticker == null) { reason = "nothing selected";    return false; }
        float cd = cooldown - (Time.time - lastShot);
        if (cd > 0f)                    { reason = $"cooldown {cd:F2}s";  return false; }
        reason = $"OK '{sb.SelectedSticker.stickerName}' x{sb.GetStickerCount(sb.SelectedSticker)}";
        return true;
    }

    // ── Arc preview ───────────────────────────────────────────────────────────
    private void DrawPreview()
    {
        var sel = sb?.SelectedSticker;
        if (sel == null) { HidePreview(); return; }

        Color col = aimOnFO
            ? Color.Lerp(sel.primaryColor, Color.white, 0.2f)
            : new Color(0.8f, 0.8f, 0.8f, 0.85f);

        if (arcLine != null)
        {
            var pts = CalcArc(ThrowOrigin(), aimPt, sel.throwForce, sel.arcHeight, arcSegments);
            arcLine.positionCount = pts.Length;
            arcLine.SetPositions(pts);
            arcLine.enabled    = true;
            arcLine.startColor = col;
            arcLine.endColor   = new Color(col.r, col.g, col.b, 0.04f);
            float w = arcWidth * (1f + Mathf.Clamp01(chargeTime / 0.4f) * 0.5f);
            arcLine.startWidth = w;
            arcLine.endWidth   = w * 0.15f;
        }

        if (marker != null)
        {
            marker.SetActive(true);
            marker.transform.position = aimPt + Vector3.up * 0.02f;
            float p = 1f + Mathf.Sin(Time.time * 8f) * 0.14f;
            float s = (0.7f + Mathf.Clamp01(chargeTime / 0.4f) * 0.5f) * p;
            marker.transform.localScale = new Vector3(0.55f * s, 0.04f, 0.55f * s);
            if (markerRend != null)
            {
                markerRend.material.color = col;
                if (markerRend.material.HasProperty("_BaseColor"))
                    markerRend.material.SetColor("_BaseColor", col);
            }
        }
    }

    private void HidePreview()
    {
        if (arcLine != null) arcLine.enabled = false;
        if (marker  != null) marker.SetActive(false);
    }

    // ── Shared arc math ───────────────────────────────────────────────────────
    public static Vector3[] CalcArc(Vector3 from, Vector3 to,
                                     float speed, float arcH, int segs)
    {
        Vector3 d  = to - from;
        Vector3 hd = new Vector3(d.x, 0f, d.z);
        float hDist = hd.magnitude;
        if (hDist < 0.05f) return new[] { from, to };

        float T  = hDist / speed;
        float g  = Mathf.Abs(Physics.gravity.y);
        float vy = (d.y / T) + (0.5f * g * T) + arcH;
        Vector3 hv = hd / hDist * speed;

        var pts = new Vector3[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float t = T * i / segs;
            pts[i] = new Vector3(from.x + hv.x * t,
                                 from.y + vy * t - 0.5f * g * t * t,
                                 from.z + hv.z * t);
        }
        return pts;
    }

    public static Vector3 CalcLaunchVelocity(Vector3 from, Vector3 to,
                                              float speed, float arcH)
    {
        Vector3 d  = to - from;
        Vector3 hd = new Vector3(d.x, 0f, d.z);
        float hDist = hd.magnitude;
        if (hDist < 0.05f) return Vector3.up * speed;

        float T  = hDist / speed;
        float g  = Mathf.Abs(Physics.gravity.y);
        float vy = (d.y / T) + (0.5f * g * T) + arcH;
        return new Vector3(hd.x / hDist * speed, vy, hd.z / hDist * speed);
    }

    // ── Sticker select ────────────────────────────────────────────────────────
    private void StickerSelect()
    {
        var m = Mouse.current;
        if (m != null)
        {
            float s = m.scroll.ReadValue().y;
            if      (s >  .01f) sb?.SelectNext();
            else if (s < -.01f) sb?.SelectPrevious();
        }
        var kb = Keyboard.current;
        if (kb != null)
        {
            Key[] keys = { Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,
                           Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9 };
            for (int i = 0; i < keys.Length; i++)
                if (kb[keys[i]].wasPressedThisFrame) { sb?.SelectStickerByIndex(i); break; }
        }
    }

    // ── Build preview ─────────────────────────────────────────────────────────
    private void BuildPreview()
    {
        var lg = new GameObject("_Arc");
        lg.transform.SetParent(transform);
        arcLine = lg.AddComponent<LineRenderer>();
        arcLine.useWorldSpace = true;
        arcLine.startWidth = arcWidth; arcLine.endWidth = arcWidth * 0.15f;
        arcLine.numCornerVertices = 6; arcLine.numCapVertices = 4;
        arcLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arcLine.receiveShadows = false;
        var lsh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default");
        if (lsh != null) arcLine.material = new Material(lsh);
        arcLine.enabled = false;

        marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "_AimMarker";
        var mc = marker.GetComponent<Collider>(); if (mc != null) mc.enabled = false;
        markerRend = marker.GetComponent<Renderer>();
        if (markerRend != null)
        {
            var ms = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (ms != null) markerRend.material = new Material(ms);
            markerRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        marker.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
        marker.SetActive(false);
    }

    // ── Runtime sphere ────────────────────────────────────────────────────────
    private static GameObject CreateSphere(StickerData d)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"S_{d.stickerName}";
        go.transform.localScale = Vector3.one * 0.55f;

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var mat = sh != null ? new Material(sh) : new Material(rend.sharedMaterial);
            mat.color = d.primaryColor;
            if (mat.HasProperty("_BaseColor"))    mat.SetColor("_BaseColor", d.primaryColor);
            if (mat.HasProperty("_EmissionColor")){ mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", d.primaryColor * 2.5f); }
            rend.material = mat;
        }

        var tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.28f; tr.startWidth = 0.25f; tr.endWidth = 0f; tr.minVertexDistance = 0.03f;
        var tsh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (tsh != null) tr.material = new Material(tsh);
        var g = new Gradient();
        g.SetKeys(new[]{new GradientColorKey(Color.white,0f), new GradientColorKey(d.primaryColor,0.25f), new GradientColorKey(d.primaryColor,1f)},
                  new[]{new GradientAlphaKey(1f,0f), new GradientAlphaKey(0.8f,0.25f), new GradientAlphaKey(0f,1f)});
        tr.colorGradient = g;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        go.AddComponent<StickerProjectile>();
        return go;
    }

    private void OnDestroy() { if (marker != null) Destroy(marker); }
}
