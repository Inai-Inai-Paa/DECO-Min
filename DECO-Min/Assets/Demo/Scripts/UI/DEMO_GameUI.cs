using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal HUD — shows sticker inventory + controls + peel hint.
/// No Canvas, no TMPro, no EventSystem needed.
/// All refs resolved lazily in OnGUI every frame.
/// </summary>
public class DEMO_GameUI : MonoBehaviour
{
    private Texture2D tPanel, tSel, tGray, tRed;
    private GUIStyle  sLabel, sSmall, sHint, sSwatch, sCount, sKey;
    private bool      texReady, styleReady;

    private void OnGUI()
    {
        EnsureTex();
        EnsureStyle();

        var sb = DEMO_StickerBook.Instance;
        if (sb == null) { Lbl(new Rect(8,8,360,28), "StickerBook not found", sLabel, tRed); return; }

        DrawInventory(sb);
        DrawControls();
        DrawPeelHint();
    }

    // ── Inventory (bottom-right) ──────────────────────────────────────────────
    private void DrawInventory(DEMO_StickerBook sb)
    {
        var list = new List<StickerEntry>();
        foreach (var e in sb.GetAllEntries()) if (e.Count > 0) list.Add(e);
        if (list.Count == 0) return;

        const float sz = 54f, pad = 5f, hdr = 22f, op = 10f;
        float pw = op * 2 + list.Count * (sz + pad) - pad;
        float ph = hdr + sz + op;
        float px = Screen.width  - pw - 8f;
        float py = Screen.height - ph - 8f;

        Box(new Rect(px, py, pw, ph), tPanel);
        GUI.Label(new Rect(px + 6, py + 3, pw - 8, hdr - 2), "STICKERS", sSmall);

        for (int i = 0; i < list.Count; i++)
        {
            var   e  = list[i];
            bool  sel = sb.SelectedSticker == e.Data;
            float sx = px + op + i * (sz + pad);
            float sy = py + hdr;

            Box(new Rect(sx, sy, sz, sz), sel ? tSel : tGray);

            var p = GUI.color;
            GUI.color = e.Data.primaryColor;
            GUI.Label(new Rect(sx + 2, sy + 1, sz - 4, sz - 18), "●", sSwatch);
            GUI.color = p;

            GUI.Label(new Rect(sx, sy + sz - 18, sz, 18), $"x{e.Count}", sCount);
            if (i < 9) GUI.Label(new Rect(sx + sz - 15, sy + 1, 14, 13), $"{i+1}", sKey);
        }

        // Selected sticker name above inventory
        if (sb.SelectedSticker != null)
        {
            string info = $"{sb.SelectedSticker.stickerName}  x{sb.GetStickerCount(sb.SelectedSticker)}  (Total:{sb.TotalCount})";
            GUI.Label(new Rect(px, py - 24, pw, 22), info, sSmall);
        }
    }

    // ── Controls (bottom-left) ────────────────────────────────────────────────
    private void DrawControls()
    {
        const float cw = 220f, ch = 144f;
        float cy = Screen.height - ch - 8f;
        Box(new Rect(8, cy, cw, ch), tPanel);
        GUI.Label(new Rect(14, cy + 4, cw - 8, ch - 6),
            "WASD         Move\n" +
            "Shift        Sprint\n" +
            "Hold LClick  Aim\n" +
            "Release      Throw\n" +
            "Scroll/1-9   Switch\n" +
            "E            Peel",
            sSmall);
    }

    // ── Peel hint (center screen) ─────────────────────────────────────────────
    private void DrawPeelHint()
    {
        var player = FindFirstObjectByType<DEMO_PlayerController>();
        if (player == null) return;

        // Check for dropped stickers (missed shots) in range
        bool nearDrop = false;
        foreach (var ds in FindObjectsByType<DEMO_DroppedSticker>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(player.transform.position, ds.transform.position) < ds.PickupRadius)
            { nearDrop = true; break; }
        }

        // Check for stickered objects in range
        bool nearPeel = false;
        foreach (var so in FindObjectsByType<DEMO_StickerizedObject>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(player.transform.position, so.transform.position) < 3.5f)
            { nearPeel = true; break; }
        }

        if (!nearDrop && !nearPeel) return;

        string msg = nearDrop ? "Press  E  to PICK UP sticker!" : "Press  E  to PEEL!";
        float w = 310f, h = 38f;
        float x = (Screen.width - w) * .5f, y = Screen.height * .56f;
        Box(new Rect(x - 4, y - 2, w + 8, h + 4), tSel);
        GUI.Label(new Rect(x, y, w, h), msg, sHint);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void Box(Rect r, Texture2D t)
    { var p = GUI.color; GUI.color = Color.white; GUI.DrawTexture(r, t); GUI.color = p; }

    private static void Lbl(Rect r, string txt, GUIStyle s, Texture2D bg)
    { Box(r, bg); GUI.Label(r, txt, s); }

    private void EnsureTex()
    {
        if (texReady) return; texReady = true;
        tPanel = T(new Color(.10f,.10f,.16f,.93f));
        tSel   = T(new Color(.90f,.74f,.08f,.93f));
        tGray  = T(new Color(.30f,.30f,.30f,.93f));
        tRed   = T(new Color(.72f,.10f,.10f,.93f));
    }

    private static Texture2D T(Color c)
    { var t = new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }

    private void EnsureStyle()
    {
        if (styleReady) return; styleReady = true;
        sLabel  = St(14, TextAnchor.MiddleLeft,   Color.white);
        sSmall  = St(11, TextAnchor.MiddleLeft,   new Color(.82f,.82f,.82f));
        sHint   = St(19, TextAnchor.MiddleCenter, Color.black, FontStyle.Bold);
        sSwatch = St(28, TextAnchor.MiddleCenter, Color.white);
        sCount  = St(11, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        sKey    = St(9,  TextAnchor.UpperRight,   new Color(1f,1f,.4f));
    }

    private static GUIStyle St(int sz, TextAnchor anchor, Color col,
                                 FontStyle fs = FontStyle.Normal)
        => new GUIStyle(GUI.skin.label)
        { fontSize=sz, fontStyle=fs, alignment=anchor, normal={textColor=col}, wordWrap=false };
}
