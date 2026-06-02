#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// FieldObject の SceneView デバッグ表示専用エディタスクリプト。
/// Assets/Scripts/Editor/ フォルダに配置すること（ビルドには含まれない）。
/// </summary>
[CustomEditor(typeof(DEMO_FieldObject))]
public class DEMO_FieldObjectEditor : Editor
{
    private void OnSceneGUI()
    {
        var fo = (DEMO_FieldObject)target;

        // シール化進捗をラベル表示
        Handles.color = fo.IsStickerized ? Color.magenta : Color.cyan;
        Handles.Label(
            fo.transform.position + Vector3.up * 2.2f,
            $"{fo.DisplayName}\n{fo.CurrentStickerPower} / {fo.StickerThreshold}"
        );

        // 進捗バーをシーンビューに描画
        float progress = fo.Progress;
        Handles.DrawWireArc(fo.transform.position, Vector3.up,
                            Vector3.forward, 360f * progress, 1.2f);
    }
}
#endif
