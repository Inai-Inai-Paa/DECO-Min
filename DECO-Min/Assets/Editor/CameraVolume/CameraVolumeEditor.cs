#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraVolume))]
public sealed class CameraVolumeEditor : Editor
{
    private const int PreviewWidth =
        320;

    private const int PreviewHeight =
        180;

    // Unityで使用可能なレイヤーは0～31
    private const int PreviewLayer =
        30;

    private Camera _previewCamera;
    private RenderTexture _previewTexture;

    private GameObject _previewObject;
    private Material _previewMaterial;

    private void OnEnable()
    {
        CreatePreviewTexture();
        CreatePreviewCamera();
        CreatePreviewObject();
    }

    private void OnDisable()
    {
        CleanupPreviewResources();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraVolume volume =
            target as CameraVolume;

        if (volume == null)
        {
            return;
        }

        EditorGUILayout.Space(
            15f);

        EditorGUILayout.LabelField(
            "Camera View Preview",
            EditorStyles.boldLabel);

        if (_previewTexture == null ||
            _previewCamera == null ||
            _previewObject == null)
        {
            EditorGUILayout.HelpBox(
                "プレビュー用リソースを作成できませんでした。",
                MessageType.Error);

            return;
        }

        /*
         * Edit ModeではRuntimeStateが生成されていないため、
         * Inspectorに設定されているStateTemplateを使用する。
         */
        CameraState cameraState =
            volume.StateTemplate;

        if (cameraState == null)
        {
            EditorGUILayout.HelpBox(
                "CameraStateが設定されていません。",
                MessageType.Info);

            return;
        }

        /*
         * Stateの種類に依存せず、
         * 各CameraStateへプレビュー計算を委譲する。
         */
        if (!cameraState.TryGetPreview(
                volume,
                out CameraPreviewData preview))
        {
            EditorGUILayout.HelpBox(
                "このCameraStateでは" +
                "プレビュー位置を計算できませんでした。",
                MessageType.Warning);

            return;
        }

        UpdatePreviewObject(
            preview.targetPosition,
            preview.targetRotation);

        UpdatePreviewCamera(
            preview.cameraPosition,
            preview.cameraRotation,
            preview.fieldOfView);

        RenderPreview();

        Rect previewRect =
            GUILayoutUtility.GetAspectRect(
                PreviewWidth /
                (float)PreviewHeight);

        GUI.DrawTexture(
            previewRect,
            _previewTexture,
            ScaleMode.ScaleToFit,
            false);

        if (!Application.isPlaying)
        {
            Repaint();
        }
    }

    private void CreatePreviewTexture()
    {
        if (_previewTexture != null)
        {
            return;
        }

        _previewTexture =
            new RenderTexture(
                PreviewWidth,
                PreviewHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                name =
                    "CameraVolume Preview Texture",

                hideFlags =
                    HideFlags.HideAndDontSave,

                antiAliasing =
                    1,

                useMipMap =
                    false,

                autoGenerateMips =
                    false
            };

        _previewTexture.Create();
    }

    private void CreatePreviewCamera()
    {
        if (_previewCamera != null)
        {
            return;
        }

        GameObject cameraObject =
            new GameObject(
                "CameraVolume Preview Camera")
            {
                hideFlags =
                    HideFlags.HideAndDontSave |
                    HideFlags.NotEditable
            };

        _previewCamera =
            cameraObject.AddComponent<Camera>();

        _previewCamera.enabled =
            false;

        _previewCamera.targetTexture =
            _previewTexture;
    }

    private void CreatePreviewObject()
    {
        if (_previewObject != null)
        {
            return;
        }

        _previewObject =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube);

        _previewObject.name =
            "CameraVolume Preview Target";

        _previewObject.hideFlags =
            HideFlags.HideAndDontSave |
            HideFlags.NotEditable;

        _previewObject.layer =
            PreviewLayer;

        Collider previewCollider =
            _previewObject.GetComponent<Collider>();

        if (previewCollider != null)
        {
            Object.DestroyImmediate(
                previewCollider);
        }

        MeshRenderer meshRenderer =
            _previewObject.GetComponent<MeshRenderer>();

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Standard");
        }

        if (shader == null)
        {
            Debug.LogWarning(
                "CameraVolumeEditor: " +
                "プレビュー用Shaderが見つかりません。");

            return;
        }

        _previewMaterial =
            new Material(
                shader)
            {
                name =
                    "CameraVolume Preview Material",

                hideFlags =
                    HideFlags.HideAndDontSave
            };

        if (_previewMaterial.HasProperty(
                "_BaseColor"))
        {
            _previewMaterial.SetColor(
                "_BaseColor",
                Color.cyan);
        }
        else if (_previewMaterial.HasProperty(
                     "_Color"))
        {
            _previewMaterial.SetColor(
                "_Color",
                Color.cyan);
        }

        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial =
                _previewMaterial;
        }
    }

    private void UpdatePreviewObject(
        Vector3 position,
        Quaternion rotation)
    {
        if (_previewObject == null)
        {
            return;
        }

        Transform previewTransform =
            _previewObject.transform;

        previewTransform.position =
            position;

        previewTransform.rotation =
            rotation;

        previewTransform.localScale =
            Vector3.one;
    }

    private void UpdatePreviewCamera(
        Vector3 cameraPosition,
        Quaternion cameraRotation,
        float fieldOfView)
    {
        if (_previewCamera == null)
        {
            return;
        }

        Transform cameraTransform =
            _previewCamera.transform;

        cameraTransform.position =
            cameraPosition;

        /*
         * 回転は各CameraStateが計算した値を使用する。
         */
        cameraTransform.rotation =
            cameraRotation;

        _previewCamera.fieldOfView =
            Mathf.Clamp(
                fieldOfView,
                1f,
                179f);

        CopyMainCameraSettings();

        _previewCamera.cullingMask |=
            1 << PreviewLayer;

        _previewCamera.targetTexture =
            _previewTexture;
    }

    private void CopyMainCameraSettings()
    {
        Camera mainCamera =
            Camera.main;

        if (mainCamera != null &&
            mainCamera != _previewCamera)
        {
            _previewCamera.clearFlags =
                mainCamera.clearFlags;

            _previewCamera.backgroundColor =
                mainCamera.backgroundColor;

            _previewCamera.cullingMask =
                mainCamera.cullingMask;

            _previewCamera.nearClipPlane =
                mainCamera.nearClipPlane;

            _previewCamera.farClipPlane =
                mainCamera.farClipPlane;

            _previewCamera.orthographic =
                mainCamera.orthographic;

            _previewCamera.orthographicSize =
                mainCamera.orthographicSize;

            return;
        }

        _previewCamera.clearFlags =
            CameraClearFlags.Skybox;

        _previewCamera.backgroundColor =
            Color.gray;

        _previewCamera.cullingMask =
            ~0;

        _previewCamera.nearClipPlane =
            0.01f;

        _previewCamera.farClipPlane =
            1000f;

        _previewCamera.orthographic =
            false;
    }

    private void RenderPreview()
    {
        if (_previewCamera == null ||
            _previewTexture == null)
        {
            return;
        }

        if (!_previewTexture.IsCreated())
        {
            _previewTexture.Create();
        }

        _previewCamera.targetTexture =
            _previewTexture;

        _previewCamera.Render();
    }

    private void CleanupPreviewResources()
    {
        /*
         * RendererがMaterialを参照しているため、
         * PreviewObjectを先に破棄する。
         */
        if (_previewObject != null)
        {
            Object.DestroyImmediate(
                _previewObject);

            _previewObject =
                null;
        }

        if (_previewMaterial != null)
        {
            Object.DestroyImmediate(
                _previewMaterial);

            _previewMaterial =
                null;
        }

        if (_previewCamera != null)
        {
            _previewCamera.targetTexture =
                null;

            Object.DestroyImmediate(
                _previewCamera.gameObject);

            _previewCamera =
                null;
        }

        if (_previewTexture != null)
        {
            if (_previewTexture.IsCreated())
            {
                _previewTexture.Release();
            }

            Object.DestroyImmediate(
                _previewTexture);

            _previewTexture =
                null;
        }
    }
}

#endif