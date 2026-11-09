using UnityEngine;
using UnityEngine.InputSystem;

public class DebugOptionsMenu : MonoBehaviour
{
    private static DebugOptionsMenu _instance;

    private Rect _windowRect = new Rect(16f, 16f, 320f, 120f);
    private bool _isOpen;
    private GUIStyle _windowStyle;
    private GUIStyle _toggleStyle;
    private GUIStyle _labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
        {
            return;
        }

        DebugOptionsMenu existing = FindAnyObjectByType<DebugOptionsMenu>();

        if (existing != null)
        {
            _instance = existing;
            return;
        }

        GameObject menuObject = new GameObject(nameof(DebugOptionsMenu));
        DontDestroyOnLoad(menuObject);
        _instance = menuObject.AddComponent<DebugOptionsMenu>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame)
        {
            _isOpen = !_isOpen;
        }
    }

    private void OnGUI()
    {
        if (!_isOpen)
        {
            return;
        }

        EnsureStyles();
        _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "デバッグオプション", _windowStyle);
    }

    private void DrawWindow(int windowId)
    {
        string state = DebugOptions.ShowEnemyVision ? "On" : "Off";
        DebugOptions.ShowEnemyVision = GUILayout.Toggle(
            DebugOptions.ShowEnemyVision,
            $"Enemyの視界 : {state}",
            _toggleStyle);

        GUILayout.Label("0キーで閉じる", _labelStyle);
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private void EnsureStyles()
    {
        if (_windowStyle != null)
        {
            return;
        }

        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic" },
            16);

        _windowStyle = new GUIStyle(GUI.skin.window);
        _toggleStyle = new GUIStyle(GUI.skin.toggle);
        _labelStyle = new GUIStyle(GUI.skin.label);

        if (font != null)
        {
            _windowStyle.font = font;
            _toggleStyle.font = font;
            _labelStyle.font = font;
        }

        _windowStyle.fontSize = 16;
        _toggleStyle.fontSize = 16;
        _toggleStyle.fixedHeight = 28f;
        _labelStyle.fontSize = 14;
    }
}
