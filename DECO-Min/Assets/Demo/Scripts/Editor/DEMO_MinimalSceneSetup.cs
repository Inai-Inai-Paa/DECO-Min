#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// メニューバー → BonbonDrop → Setup Minimal Test Scene
/// を実行するだけでテストプレイ可能なシーンを自動構築するエディタツール。
///
/// 生成されるオブジェクト：
///   ・Directional Light
///   ・Ground（Plane）
///   ・FieldObjects × 3（Cube）
///   ・Player（Capsule + 各スクリプト）
///   ・Main Camera（CameraController）
///   ・Managers（GameManager, StickerBook, UIManager 等）
///   ・Canvas（最小限の UI）
/// </summary>
public static class DEMO_MinimalSceneSetup
{
    [MenuItem("BonbonDrop/Setup Minimal Test Scene")]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("テストシーン生成",
            "現在のシーンにオブジェクトを追加します。続けますか？", "OK", "キャンセル"))
            return;

        CreateLight();
        CreateGround();
        GameObject player = CreatePlayer();
        CreateCamera(player.transform);
        CreateManagers();
        CreateFieldObjects();
        CreateMinimalUI();

        Debug.Log("[MinimalSceneSetup] テストシーンの構築が完了しました！\n" +
                  "Play ボタンを押す前に StickerBook の StartingStickers に\n" +
                  "StickerData アセットを割り当ててください。");
    }

    // ─── ライト ───────────────────────────────────────────────────────────────
    private static void CreateLight()
    {
        if (FindOrNull<Light>() != null) return;

        var go   = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1.2f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ─── 地面 ─────────────────────────────────────────────────────────────────
    private static void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name                  = "Ground";
        ground.transform.localScale  = new Vector3(5f, 1f, 5f);
        ground.layer                 = LayerMask.NameToLayer("Default");

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat != null)
        {
            mat.color = new Color(0.4f, 0.65f, 0.3f);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    // ─── プレイヤー ───────────────────────────────────────────────────────────
    private static GameObject CreatePlayer()
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name              = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.layer             = LayerMask.NameToLayer("Default");

        // CharacterController（コライダーと競合するためデフォルト Capsule Collider を削除）
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0f, 0f);

        player.AddComponent<DEMO_PlayerController>();
        player.AddComponent<DEMO_StickerThrower>();

        // 接地チェック用の子 GameObject（PlayerController が自動生成するが念のため）
        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.9f, 0f);

        Debug.Log("[MinimalSceneSetup] Player 生成完了");
        return player;
    }

    // ─── カメラ ───────────────────────────────────────────────────────────────
    private static void CreateCamera(Transform playerTransform)
    {
        // 既存の MainCamera があれば流用
        var cam = Camera.main != null
            ? Camera.main.gameObject
            : new GameObject("Main Camera");

        cam.tag = "MainCamera";
        if (cam.GetComponent<Camera>() == null) cam.AddComponent<Camera>();

        var ctrl = cam.GetComponent<DEMO_CameraController>() ?? cam.AddComponent<DEMO_CameraController>();
        // target は実行時に PlayerController を自動検索するので設定不要

        cam.transform.position = new Vector3(0f, 7f, -10f);
        cam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

        Debug.Log("[MinimalSceneSetup] Camera 生成完了");
    }

    // ─── マネージャー ─────────────────────────────────────────────────────────
    private static void CreateManagers()
    {
        EnsureManager<DEMO_GameManager>("GameManager");
        EnsureManager<DEMO_StickerBook>("StickerBook");
        // UIManager は Canvas 内に配置するため後で作成
    }

    private static void EnsureManager<T>(string goName) where T : Component
    {
        if (FindOrNull<T>() != null) return;
        var go = new GameObject(goName);
        go.AddComponent<T>();
        Debug.Log($"[MinimalSceneSetup] {goName} 生成完了");
    }

    // ─── フィールドオブジェクト ───────────────────────────────────────────────
    private static void CreateFieldObjects()
    {
        Vector3[] positions = {
            new(-3f, 0.5f, 3f),
            new( 0f, 0.5f, 5f),
            new( 4f, 0.5f, 2f)
        };

        int[] thresholds = { 3, 5, 8 };
        string[] names   = { "小さな石", "木の切り株", "古い壺" };
        Color[] colors   = {
            new(0.6f, 0.6f, 0.6f),
            new(0.5f, 0.3f, 0.1f),
            new(0.8f, 0.7f, 0.5f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = names[i];
            cube.transform.position = positions[i];
            cube.layer = LayerMask.NameToLayer("Default");

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat != null)
            {
                mat.color = colors[i];
                cube.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var fo = cube.AddComponent<DEMO_FieldObject>();

            // SerializedObject 経由でプライベートフィールドを設定
            var so = new SerializedObject(fo);
            so.FindProperty("objectDisplayName").stringValue = names[i];
            so.FindProperty("stickerThreshold").intValue     = thresholds[i];
            so.FindProperty("rewardCount").intValue          = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("[MinimalSceneSetup] FieldObjects × 3 生成完了");
    }

    // ─── 最小限の UI ─────────────────────────────────────────────────────────
    private static void CreateMinimalUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder   = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // UIManager
        var uiManagerGO = new GameObject("UIManager");
        uiManagerGO.transform.SetParent(canvasGO.transform, false);
        uiManagerGO.AddComponent<DEMO_UIManager>();

        // 探索HUD（シンプルテキスト）
        var hudPanel = CreatePanel(canvasGO.transform, "ExplorationHUD");
        var hud      = hudPanel.AddComponent<DEMO_ExplorationHUD>();

        var timerText = CreateText(hudPanel.transform, "TimerText", "05:00",
                                   new Vector2(0f, 1f), new Vector2(-10f, -10f));
        timerText.fontSize = 36;
        timerText.fontStyle = FontStyles.Bold;

        // SerializedObject 経由で ExplorationHUD にタイマーテキストを設定
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("timerText").objectReferenceValue = timerText;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        // シール帳パネル
        var bookPanel = CreatePanel(canvasGO.transform, "StickerBookPanel");
        var bookUI    = bookPanel.AddComponent<DEMO_StickerBookUI>();

        // 帰還パネル
        var returnPanel = CreatePanel(canvasGO.transform, "ReturnPanel");
        CreateText(returnPanel.transform, "ReturnText", "帰還中...",
                   new Vector2(0.5f, 0.5f), Vector2.zero);

        // UIManager に参照を接続
        var uiSO = new SerializedObject(uiManagerGO.GetComponent<DEMO_UIManager>());
        uiSO.FindProperty("stickerBookPanel").objectReferenceValue = bookPanel;
        uiSO.FindProperty("explorationPanel").objectReferenceValue = hudPanel;
        uiSO.FindProperty("returnPanel").objectReferenceValue      = returnPanel;
        uiSO.FindProperty("stickerBookUI").objectReferenceValue    = bookUI;
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        // EventSystem（なければ）
        if (FindOrNull<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // 新 Input System を使っている場合は InputSystemUIInputModule が必要。
            // 旧 Input System（Both モード含む）の場合は StandaloneInputModule を使うこと。
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        Debug.Log("[MinimalSceneSetup] Canvas / UI 生成完了");
    }

    // ─── UI ヘルパー ──────────────────────────────────────────────────────────
    private static GameObject CreatePanel(Transform parent, string name)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.AddComponent<CanvasGroup>();
        return go;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
                                              Vector2 anchor, Vector2 offset)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(300f, 60f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = 24;
        tmp.color    = Color.white;
        return tmp;
    }

    private static T FindOrNull<T>() where T : Object =>
        Object.FindFirstObjectByType<T>();
}
#endif
