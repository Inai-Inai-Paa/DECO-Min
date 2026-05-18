#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// BonbonDrop > Build Complete Playable Scene
/// Clears scene and builds a fully playable prototype.
/// Press Play immediately after running — no extra setup needed.
/// </summary>
public static class BonbonDropSceneBuilder
{
    [MenuItem("BonbonDrop/Build Complete Playable Scene")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog("Build Scene",
            "Clear the current scene and build BonbonDrop prototype?",
            "Build", "Cancel")) return;

        ClearScene();

        MakeLight();
        MakeGround();
        MakePlayer();
        MakeCamera();
        MakeManagers();
        MakeFieldObjects();
        MakeGameUI();
        MakeBootstrap();

        EditorUtility.DisplayDialog("Done!",
            "Scene ready. Press ▶ Play.\n\n" +
            "WASD = Move | Hold LClick = Aim | Release = Throw | E = Peel",
            "Play!");
    }

    // ── Clear ─────────────────────────────────────────────────────────────────
    static void ClearScene()
    {
        // Collect root objects first, THEN destroy.
        // Destroying a parent immediately destroys its children too, so any
        // child that appeared in FindObjectsByType would throw when accessed.
        var roots = new System.Collections.Generic.List<GameObject>();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            try
            {
                if (go.transform.parent == null)
                    roots.Add(go);
            }
            catch { /* already destroyed */ }
        }
        foreach (var go in roots)
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }
    }

    // ── Light ─────────────────────────────────────────────────────────────────
    static void MakeLight()
    {
        var go = new GameObject("Directional Light");
        var l  = go.AddComponent<Light>();
        l.type = LightType.Directional; l.intensity = 1.2f; l.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
    }

    // ── Ground ────────────────────────────────────────────────────────────────
    static void MakeGround()
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "Ground"; g.layer = 0;
        g.transform.localScale = new Vector3(4f, 1f, 4f);
        SetColor(g, new Color(.35f, .55f, .28f));
    }

    // ── Player ────────────────────────────────────────────────────────────────
    static void MakePlayer()
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        p.name = "Player"; p.layer = 0;
        p.transform.position = new Vector3(0f, 1.01f, 0f);
        SetColor(p, new Color(.3f, .55f, .9f));
        Object.DestroyImmediate(p.GetComponent<CapsuleCollider>());

        var cc = p.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero;

        p.AddComponent<PlayerController>();
        p.AddComponent<StickerThrower>();
    }

    // ── Camera ────────────────────────────────────────────────────────────────
    static void MakeCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.AddComponent<Camera>().fieldOfView = 60f;
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraController>();
        go.transform.position = new Vector3(0f, 6f, -9f);
        go.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
    }

    // ── Managers ─────────────────────────────────────────────────────────────
    static void MakeManagers()
    {
        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("StickerBook").AddComponent<StickerBook>();
    }

    // ── Field Objects ─────────────────────────────────────────────────────────
    static void MakeFieldObjects()
    {
        var parent = new GameObject("FieldObjects");
        var defs = new (string n, Vector3 pos, int thr, Color col)[]
        {
            ("Pebble",   new Vector3(-5f, .5f,  5f), 3, new Color(.6f, .6f, .6f)),
            ("Block",    new Vector3( 5f, .6f,  5f), 3, new Color(.55f,.35f,.15f)),
            ("Vase",     new Vector3( 0f, .8f,  9f), 5, new Color(.65f,.55f,.30f)),
            ("Box",      new Vector3(-6f, .5f, -4f), 5, new Color(.40f,.20f,.55f)),
            ("Relic",    new Vector3( 6f, .75f,-4f), 8, new Color(.20f,.45f,.75f)),
        };

        foreach (var d in defs)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = d.n; c.layer = 0;
            c.transform.SetParent(parent.transform);
            c.transform.position = d.pos;
            SetColor(c, d.col);

            var fo   = c.AddComponent<FieldObject>();
            var so   = new SerializedObject(fo);
            so.FindProperty("objectDisplayName").stringValue = d.n;
            so.FindProperty("stickerThreshold").intValue     = d.thr;
            so.FindProperty("rewardCount").intValue          = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ── GameUI ────────────────────────────────────────────────────────────────
    static void MakeGameUI()
    {
        new GameObject("GameUI").AddComponent<GameUI>();
    }

    // ── Bootstrap ─────────────────────────────────────────────────────────────
    static void MakeBootstrap()
    {
        var go = new GameObject("Bootstrap");
        go.AddComponent<SampleBootstrap>();
        go.AddComponent<InputDiagnostic>(); // shows raw mouse state — remove when input is working
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static void SetColor(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;

        var sh  = Shader.Find("Universal Render Pipeline/Lit")
               ?? Shader.Find("Standard")
               ?? Shader.Find("Diffuse");

        // sh と r.sharedMaterial は型が違うので ?? では繋げない → if で分岐
        var mat = sh != null
            ? new Material(sh)
            : new Material(r.sharedMaterial);

        mat.color = c;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        r.sharedMaterial = mat;
    }
}
#endif
