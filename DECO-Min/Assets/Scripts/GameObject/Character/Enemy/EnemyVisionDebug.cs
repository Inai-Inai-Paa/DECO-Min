using UnityEngine;
using UnityEngine.Rendering;

public partial class Enemy
{
    private const int VisionDebugSegments = 28;
    private const float VisionDebugHeight = 0.12f;
    private const string VisionDebugShaderName = "DECO/Debug/EnemyVision";

    private static bool _missingVisionShaderLogged;

    private GameObject _visionDebugObject;
    private MeshFilter _visionDebugFilter;
    private Mesh _visionDebugMesh;
    private Material _visionDebugMaterial;
    private float _visionDebugAngle = -1f;
    private float _visionDebugRange = -1f;

    private void UpdateVisionDebugView()
    {
        if (!DebugOptions.ShowEnemyVision)
        {
            if (_visionDebugObject != null && _visionDebugObject.activeSelf)
            {
                _visionDebugObject.SetActive(false);
            }

            return;
        }

        if (!EnsureVisionDebugView())
        {
            return;
        }

        if (!_visionDebugObject.activeSelf)
        {
            _visionDebugObject.SetActive(true);
        }

        RefreshVisionDebugMesh();
        ApplyVisionDebugColor(GetVisionDebugColor());
    }

    private void ReleaseVisionDebugView()
    {
        if (_visionDebugMesh != null)
        {
            Destroy(_visionDebugMesh);
            _visionDebugMesh = null;
        }

        if (_visionDebugMaterial != null)
        {
            Destroy(_visionDebugMaterial);
            _visionDebugMaterial = null;
        }

        _visionDebugObject = null;
        _visionDebugFilter = null;
        _visionDebugAngle = -1f;
        _visionDebugRange = -1f;
    }

    private bool EnsureVisionDebugView()
    {
        if (_visionDebugObject != null)
        {
            return _visionDebugMaterial != null;
        }

        Shader shader = Resources.Load<Shader>("Debug/EnemyVision");

        if (shader == null)
        {
            shader = Shader.Find(VisionDebugShaderName);
        }

        if (shader == null)
        {
            if (!_missingVisionShaderLogged)
            {
                _missingVisionShaderLogged = true;
                Debug.LogError($"Shader '{VisionDebugShaderName}' was not found.");
            }

            return false;
        }

        _visionDebugObject = new GameObject("VisionDebug");
        _visionDebugObject.transform.SetParent(transform, false);

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");

        if (ignoreRaycast >= 0)
        {
            _visionDebugObject.layer = ignoreRaycast;
        }

        _visionDebugFilter = _visionDebugObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = _visionDebugObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        _visionDebugMaterial = new Material(shader);
        renderer.sharedMaterial = _visionDebugMaterial;
        return true;
    }

    private void RefreshVisionDebugMesh()
    {
        float angle = Mathf.Clamp(_viewAngle, 1f, 360f);
        float range = Mathf.Max(0.1f, _alertDistance);

        if (_visionDebugMesh != null
            && Mathf.Approximately(angle, _visionDebugAngle)
            && Mathf.Approximately(range, _visionDebugRange))
        {
            return;
        }

        _visionDebugAngle = angle;
        _visionDebugRange = range;

        int arcCount = VisionDebugSegments;
        var vertices = new Vector3[arcCount + 2];
        var triangles = new int[arcCount * 3];
        float halfAngle = angle * 0.5f;
        Vector3 origin = new Vector3(0f, VisionDebugHeight, 0f);

        vertices[0] = origin;

        for (int i = 0; i <= arcCount; i++)
        {
            float yaw = Mathf.Lerp(-halfAngle, halfAngle, i / (float)arcCount);
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            vertices[i + 1] = origin + direction * range;
        }

        for (int i = 0; i < arcCount; i++)
        {
            int offset = i * 3;
            triangles[offset] = 0;
            triangles[offset + 1] = i + 1;
            triangles[offset + 2] = i + 2;
        }

        if (_visionDebugMesh == null)
        {
            _visionDebugMesh = new Mesh { name = "EnemyVision" };
            _visionDebugMesh.MarkDynamic();
        }

        _visionDebugMesh.Clear();
        _visionDebugMesh.vertices = vertices;
        _visionDebugMesh.triangles = triangles;
        _visionDebugMesh.RecalculateBounds();
        _visionDebugFilter.sharedMesh = _visionDebugMesh;
    }

    private Color GetVisionDebugColor()
    {
        bool seesTarget = HasTarget()
            && IsTargetInAlertDistance()
            && IsTargetInFieldOfView()
            && HasLineOfSightToTarget();

        if (seesTarget)
        {
            return new Color(1f, 0.22f, 0.15f, 0.42f);
        }

        if (Awareness == EnemyAwareness.Engaged)
        {
            return new Color(1f, 0.62f, 0.12f, 0.36f);
        }

        return new Color(0.2f, 0.78f, 1f, 0.34f);
    }

    private void ApplyVisionDebugColor(Color color)
    {
        if (_visionDebugMaterial.HasProperty("_BaseColor"))
        {
            _visionDebugMaterial.SetColor("_BaseColor", color);
        }

        if (_visionDebugMaterial.HasProperty("_Color"))
        {
            _visionDebugMaterial.SetColor("_Color", color);
        }
    }
}
