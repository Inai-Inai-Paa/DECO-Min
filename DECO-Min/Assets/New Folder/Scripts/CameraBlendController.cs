using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraBlendController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Default Camera")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 5f, -10f);
    [SerializeField] private float defaultFov = 60f;
    [SerializeField] private float defaultSmoothness = 5f;

    [Header("Blend")]
    [SerializeField] private float blendSpeed = 5f;

    private Camera cam;

    private Vector3 currentOffset;
    private float currentFov;
    private float currentSmoothness;

    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        currentOffset = defaultOffset;
        currentFov = defaultFov;
        currentSmoothness = defaultSmoothness;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        CameraBlendVolume.CameraParams blendedParams = GetBlendedCameraParams();

        // 補間
        currentOffset = Vector3.Lerp(
            currentOffset,
            blendedParams.offset,
            Time.deltaTime * blendSpeed);

        currentFov = Mathf.Lerp(
            currentFov,
            blendedParams.fieldOfView,
            Time.deltaTime * blendSpeed);

        currentSmoothness = Mathf.Lerp(
            currentSmoothness,
            blendedParams.smoothness,
            Time.deltaTime * blendSpeed);

        // 目標位置
        Vector3 desiredPosition =
            target.position +
            (target.rotation * currentOffset);

        // スムーズ移動
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * currentSmoothness);

        // 注視
        transform.LookAt(target.position);

        // FOV
        cam.fieldOfView = currentFov;
    }

    private CameraBlendVolume.CameraParams GetBlendedCameraParams()
    {
        CameraBlendVolume[] volumes =
            FindObjectsOfType<CameraBlendVolume>();

        if (volumes.Length == 0)
        {
            return new CameraBlendVolume.CameraParams
            {
                offset = defaultOffset,
                fieldOfView = defaultFov,
                smoothness = defaultSmoothness
            };
        }

        Vector3 targetPos = target.position;

        float totalWeight = 0f;

        Vector3 blendedOffset = Vector3.zero;
        float blendedFov = 0f;
        float blendedSmoothness = 0f;

        foreach (var volume in volumes)
        {
            float distance =
                Vector3.Distance(targetPos, volume.transform.position);

            if (distance > volume.radius)
                continue;

            // 中心ほど強く影響
            float weight =
                1f - Mathf.Clamp01(distance / volume.radius);

            totalWeight += weight;

            blendedOffset += volume.config.offset * weight;
            blendedFov += volume.config.fieldOfView * weight;
            blendedSmoothness += volume.config.smoothness * weight;
        }

        // Volume外
        if (totalWeight <= 0.0001f)
        {
            return new CameraBlendVolume.CameraParams
            {
                offset = defaultOffset,
                fieldOfView = defaultFov,
                smoothness = defaultSmoothness
            };
        }

        blendedOffset /= totalWeight;
        blendedFov /= totalWeight;
        blendedSmoothness /= totalWeight;

        return new CameraBlendVolume.CameraParams
        {
            offset = blendedOffset,
            fieldOfView = blendedFov,
            smoothness = blendedSmoothness
        };
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (target == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, target.position);
    }
#endif
}