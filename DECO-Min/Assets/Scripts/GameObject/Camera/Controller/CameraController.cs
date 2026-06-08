using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Player _player;

    private List<CameraVolume> _volumes;

    private void Awake()
    {
        _volumes = new List<CameraVolume>(Object.FindObjectsByType<CameraVolume>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        if (_player == null) return;

        Vector3 playerPos = _player.transform.position;

        Vector3 blendedPosition = Vector3.zero;
        Vector3 blendedTargetPos = Vector3.zero;

        float totalWeight = 0f;

        foreach (var volume in _volumes)
        {
            float distance =
                Vector3.Distance(playerPos, volume.transform.position);

            if (distance >= volume.radius)
                continue;

            float weight = 1f - (distance / volume.radius);

            totalWeight += weight;

            if (volume.followPlayer)
            {
                Vector3 camPos =
                    playerPos +
                    (volume.transform.rotation * volume.config.offset);

                blendedPosition += camPos * weight;
                blendedTargetPos += playerPos * weight;
            }
            else
            {
                if (volume.GetPreviewTransform(
                    out Vector3 camPos,
                    out Quaternion camRot,
                    out Vector3 targetPos))
                {
                    blendedPosition += camPos * weight;
                    blendedTargetPos += targetPos * weight;
                }
            }
        }

        if (totalWeight <= 0f)
            return;

        blendedPosition /= totalWeight;
        blendedTargetPos /= totalWeight;

        Transform cam = Camera.main.transform;

        // ˆÊ’u•âŠÔ
        cam.position = Vector3.Lerp(
            cam.position,
            blendedPosition,
            Time.deltaTime * 5f);

        // LookRotation ‚ðŽg‚Á‚Ä‰ñ“]¶¬
        Vector3 forward =
            (blendedTargetPos - cam.position).normalized;

        if (forward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot =
                Quaternion.LookRotation(forward);

            cam.rotation = Quaternion.Slerp(
                cam.rotation,
                targetRot,
                Time.deltaTime * 5f);
        }
    }
}