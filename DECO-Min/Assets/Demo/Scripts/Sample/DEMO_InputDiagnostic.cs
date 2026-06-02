using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Realtime input + phase diagnostic.  Remove once confirmed working.
/// Press F key at any time to force-start exploration.
/// </summary>
public class DEMO_InputDiagnostic : MonoBehaviour
{
    private string line1 = "";
    private string line2 = "";

    private void Update()
    {
        var mouse = Mouse.current;
        var gm    = DEMO_GameManager.Instance;

        string phase = gm != null ? gm.CurrentPhase.ToString() : "NO GameManager!";
        line1 = $"Phase: {phase}";

        if (mouse != null)
        {
            bool ip = mouse.leftButton.isPressed;
            line2 = $"Mouse.isPressed={ip}  pos={mouse.position.ReadValue()}";
            if (ip) Debug.Log($"[Diag] isPressed=True  phase={phase}");
        }
        else
        {
            line2 = "Mouse.current = NULL  ← Input System not active!";
        }

        // F key = force start exploration from anywhere
        if (Keyboard.current?.fKey.wasPressedThisFrame == true)
        {
            Debug.Log("[Diag] F key → forcing StartExploration");
            gm?.StartExploration();
        }
    }

    private void OnGUI()
    {
        // Bright background so always readable
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(5, Screen.height - 75, 420, 68), Texture2D.whiteTexture);

        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, Screen.height - 72, 410, 22), line1);
        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, Screen.height - 50, 410, 22), line2);
        GUI.color = Color.white;
        GUI.Label(new Rect(10, Screen.height - 28, 410, 22), "F key = force start exploration");
    }
}
