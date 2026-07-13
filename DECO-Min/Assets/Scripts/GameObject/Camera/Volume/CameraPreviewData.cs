using UnityEngine;

/// <summary>
/// CameraStateからEditorプレビューへ渡すカメラ情報。
/// </summary>
public struct CameraPreviewData
{
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;

    public Vector3 targetPosition;
    public Quaternion targetRotation;

    public float fieldOfView;
}