#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CameraVolume))]
public class CameraBlendVolumeEditor : Editor
{
    private Camera previewCamera;
    private RenderTexture previewTexture;

    // Preview描画用
    private GameObject previewObject;

    // Preview専用Layer
    private const int PreviewLayer = 30;

    private void OnEnable()
    {
        // プレビュー用RT
        previewTexture = new RenderTexture(320, 180, 24, RenderTextureFormat.ARGB32);

        CreatePreviewObject();
    }

    private void OnDisable()
    {
        // Cleanup
        if (previewCamera != null)
            DestroyImmediate(previewCamera.gameObject);

        if (previewTexture != null)
            DestroyImmediate(previewTexture);

        if (previewObject != null)
            DestroyImmediate(previewObject);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraVolume volume = target as CameraVolume;
        if (volume == null)
            return;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Camera View Preview", EditorStyles.boldLabel);

        // カメラ位置計算
        if (volume.GetPreviewTransform(out Vector3 pos, out Quaternion rot, out Vector3 targetPos))
        {
            // カメラ更新
            UpdatePreviewCamera(pos, rot, volume.config.fieldOfView);

            // 仮オブジェクト更新
            UpdatePreviewObject(targetPos, rot);

            // Render
            previewCamera.targetTexture = previewTexture;
            previewCamera.Render();

            // GUI表示
            Rect rect = GUILayoutUtility.GetRect(320, 180, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit);

            Repaint();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "地面（Collider）が真下に見つかりません。\n" +
                "オブジェクトの下に床を配置するか、位置を調整してください。",
                MessageType.Warning
            );
        }
    }

    private void CreatePreviewObject()
    {
        previewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        previewObject.name = "Preview Temp Object";

        previewObject.hideFlags =
            HideFlags.HideAndDontSave |
            HideFlags.NotEditable;

        previewObject.layer = PreviewLayer;

        // Collider不要
        Collider col = previewObject.GetComponent<Collider>();
        if (col != null)
            DestroyImmediate(col);

        // マテリアル
        MeshRenderer renderer = previewObject.GetComponent<MeshRenderer>();

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.cyan;

        renderer.sharedMaterial = mat;
    }

    private void UpdatePreviewObject(Vector3 pos, Quaternion rot)
    {
        if (previewObject == null)
            return;

        previewObject.transform.position = pos;
        previewObject.transform.rotation = rot;
        previewObject.transform.localScale = Vector3.one;
    }

    private void UpdatePreviewCamera(Vector3 pos, Quaternion rot, float fov)
    {
        if (previewCamera == null)
        {
            GameObject camGO = new GameObject("Volume Preview Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            previewCamera = camGO.AddComponent<Camera>();

            previewCamera.enabled = false;
        }

        previewCamera.transform.position = pos;
        previewCamera.transform.rotation = rot;

        previewCamera.fieldOfView = fov;

        // MainCamera設定コピー
        if (Camera.main != null)
        {
            previewCamera.clearFlags = Camera.main.clearFlags;
            previewCamera.backgroundColor = Camera.main.backgroundColor;
            previewCamera.cullingMask = Camera.main.cullingMask;
            previewCamera.nearClipPlane = Camera.main.nearClipPlane;
            previewCamera.farClipPlane = Camera.main.farClipPlane;
        }

        // PreviewLayer追加
        previewCamera.cullingMask |= (1 << PreviewLayer);
    }
}
#endif