using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CameraBlendState",
    menuName = "State/Camera/Blend")]
public sealed class CameraBlendState : CameraState
{
    public enum BlendAnchorMode
    {
        PlayerRelative,
        GroundRelative
    }

    [Header("Blend Behavior")]

    [SerializeField]
    private BlendAnchorMode _anchorMode =
        BlendAnchorMode.PlayerRelative;

    private readonly List<BlendSample> _blendSamples =
        new List<BlendSample>();

    public BlendAnchorMode AnchorMode =>
        _anchorMode;

    public override void Initialize(
        CameraController controller,
        Camera camera,
        StateMachine stateMachine,
        Transform target,
        CameraVolume volume)
    {
        base.Initialize(
            controller,
            camera,
            stateMachine,
            target,
            volume);

        stateName =
            nameof(CameraBlendState);
    }

    public override void Update()
    {
        if (_controller == null ||
            _camera == null ||
            _target == null)
        {
            return;
        }

        IReadOnlyList<CameraVolume> volumes =
            _controller.Volumes;

        if (volumes == null ||
            volumes.Count == 0)
        {
            return;
        }

        _blendSamples.Clear();

        Vector3 playerPosition =
            _target.position;

        Vector3 weightedOrigin =
            Vector3.zero;

        Vector3 weightedOffset =
            Vector3.zero;

        Vector3 weightedForward =
            Vector3.zero;

        Vector3 weightedUp =
            Vector3.zero;

        Vector3 weightedRotationLimitMin =
            Vector3.zero;

        Vector3 weightedRotationLimitMax =
            Vector3.zero;

        float weightedFieldOfView =
            0f;

        float weightedSmoothness =
            0f;

        float totalWeight =
            0f;

        /*
         * Volumeごとの最終カメラ位置はまだ計算しない。
         *
         * 先に基準姿勢・Origin・Offset・制限値を
         * それぞれ1つの値へブレンドする。
         */
        for (int i = 0;
             i < volumes.Count;
             ++i)
        {
            CameraVolume volume =
                volumes[i];

            if (!TryGetBlendSample(
                    volume,
                    playerPosition,
                    out BlendSample sample))
            {
                continue;
            }

            _blendSamples.Add(
                sample);

            weightedOrigin +=
                sample.origin *
                sample.weight;

            weightedOffset +=
                sample.offset *
                sample.weight;

            weightedForward +=
                sample.baseForward *
                sample.weight;

            weightedUp +=
                sample.baseUp *
                sample.weight;

            weightedRotationLimitMin +=
                sample.rotationLimitMin *
                sample.weight;

            weightedRotationLimitMax +=
                sample.rotationLimitMax *
                sample.weight;

            weightedFieldOfView +=
                sample.fieldOfView *
                sample.weight;

            weightedSmoothness +=
                sample.smoothness *
                sample.weight;

            totalWeight +=
                sample.weight;
        }

        if (totalWeight <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 blendedOrigin =
            weightedOrigin /
            totalWeight;

        Vector3 blendedOffset =
            weightedOffset /
            totalWeight;

        Vector3 blendedForward =
            weightedForward /
            totalWeight;

        Vector3 blendedUp =
            weightedUp /
            totalWeight;

        Vector3 rotationLimitMin =
            weightedRotationLimitMin /
            totalWeight;

        Vector3 rotationLimitMax =
            weightedRotationLimitMax /
            totalWeight;

        float targetFieldOfView =
            weightedFieldOfView /
            totalWeight;

        float smoothness =
            weightedSmoothness /
            totalWeight;

        /*
         * 多数のVolumeから、単一の安定した基準姿勢を生成する。
         */
        Quaternion blendedBaseRotation =
            CreateBlendedBaseRotation(
                blendedForward,
                blendedUp);

        /*
         * 入力は一度だけ更新する。
         *
         * VolumeごとにOrbit回転を適用しないことが重要。
         */
        Vector3 orbitAngles =
            _controller.UpdateOrbitAngles(
                rotationLimitMin,
                rotationLimitMax,
                smoothness,
                Time.deltaTime);

        Quaternion finalOrbitRotation =
            CameraController.CreateCameraOrbitRotation(
                blendedBaseRotation,
                orbitAngles);

        Vector3 targetCameraPosition =
            blendedOrigin +
            finalOrbitRotation *
            blendedOffset;

        ApplyCameraTransform(
            targetCameraPosition,
            blendedOrigin,
            targetFieldOfView,
            smoothness);
    }

    private bool TryGetBlendSample(
        CameraVolume volume,
        Vector3 playerPosition,
        out BlendSample sample)
    {
        sample = default;

        if (volume == null ||
            !volume.isActiveAndEnabled)
        {
            return false;
        }

        CameraBlendState blendState =
            volume.RuntimeState
                as CameraBlendState;

        if (blendState == null)
        {
            return false;
        }

        float radius =
            volume.Radius;

        if (radius <= Mathf.Epsilon)
        {
            return false;
        }

        Vector3 difference =
            playerPosition -
            volume.transform.position;

        float squaredDistance =
            difference.sqrMagnitude;

        if (squaredDistance >=
            radius * radius)
        {
            return false;
        }

        float distance =
            Mathf.Sqrt(
                squaredDistance);

        float weight =
            1f -
            distance /
            radius;

        if (weight <= Mathf.Epsilon)
        {
            return false;
        }

        Vector3 origin;

        switch (blendState.AnchorMode)
        {
            case BlendAnchorMode.PlayerRelative:
                {
                    origin =
                        playerPosition;

                    break;
                }

            case BlendAnchorMode.GroundRelative:
                {
                    if (!volume.GetGroundPosition(
                            out origin))
                    {
                        return false;
                    }

                    break;
                }

            default:
                return false;
        }

        CameraVolume.CameraParams config =
            volume.Config;

        Quaternion baseRotation =
            volume.transform.rotation;

        sample = new BlendSample
        {
            origin =
                origin,

            offset =
                config.offset,

            baseForward =
                baseRotation *
                Vector3.forward,

            baseUp =
                baseRotation *
                Vector3.up,

            rotationLimitMin =
                config.rotationLimitMin,

            rotationLimitMax =
                config.rotationLimitMax,

            fieldOfView =
                Mathf.Clamp(
                    config.fieldOfView,
                    1f,
                    179f),

            smoothness =
                Mathf.Max(
                    0f,
                    config.smoothness),

            weight =
                weight
        };

        return true;
    }

    /// <summary>
    /// ブレンドされたForwardとUpから、
    /// 直交した安定姿勢を生成する。
    /// </summary>
    private static Quaternion CreateBlendedBaseRotation(
        Vector3 blendedForward,
        Vector3 blendedUp)
    {
        if (blendedForward.sqrMagnitude <=
            0.000001f)
        {
            blendedForward =
                Vector3.forward;
        }

        blendedForward.Normalize();

        /*
         * UpからForward方向の成分を除去し、
         * 互いに直交させる。
         */
        Vector3 correctedUp =
            Vector3.ProjectOnPlane(
                blendedUp,
                blendedForward);

        if (correctedUp.sqrMagnitude <=
            0.000001f)
        {
            correctedUp =
                Vector3.ProjectOnPlane(
                    Vector3.up,
                    blendedForward);
        }

        if (correctedUp.sqrMagnitude <=
            0.000001f)
        {
            correctedUp =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
                    blendedForward);
        }

        correctedUp.Normalize();

        return Quaternion.LookRotation(
            blendedForward,
            correctedUp);
    }

    private void ApplyCameraTransform(
        Vector3 targetCameraPosition,
        Vector3 targetLookPosition,
        float targetFieldOfView,
        float smoothness)
    {
        float interpolationRate =
            smoothness <= Mathf.Epsilon
                ? 1f
                : 1f -
                  Mathf.Exp(
                      -smoothness *
                      Time.deltaTime);

        Transform cameraTransform =
            _camera.transform;

        cameraTransform.position =
            Vector3.Lerp(
                cameraTransform.position,
                targetCameraPosition,
                interpolationRate);

        /*
         * カメラの姿勢は、補間後の位置からOriginを見る。
         *
         * UpはワールドY方向に固定するため、
         * 意図しないZ軸傾斜も抑制される。
         */
        Vector3 forward =
            targetLookPosition -
            cameraTransform.position;

        if (forward.sqrMagnitude >
            0.000001f)
        {
            forward.Normalize();

            Quaternion targetRotation =
                CreateStableLookRotation(
                    forward,
                    cameraTransform.rotation);

            cameraTransform.rotation =
                Quaternion.Slerp(
                    cameraTransform.rotation,
                    targetRotation,
                    interpolationRate);
        }

        if (!_camera.orthographic)
        {
            _camera.fieldOfView =
                Mathf.Lerp(
                    _camera.fieldOfView,
                    targetFieldOfView,
                    interpolationRate);
        }
    }

    private static Quaternion CreateStableLookRotation(
        Vector3 forward,
        Quaternion fallbackRotation)
    {
        if (forward.sqrMagnitude <=
            0.000001f)
        {
            return fallbackRotation;
        }

        forward.Normalize();

        if (Mathf.Abs(
                Vector3.Dot(
                    forward,
                    Vector3.up)) <
            0.999f)
        {
            return Quaternion.LookRotation(
                forward,
                Vector3.up);
        }

        Vector3 right =
            fallbackRotation *
            Vector3.right;

        right =
            Vector3.ProjectOnPlane(
                right,
                forward);

        if (right.sqrMagnitude <=
            0.000001f)
        {
            right =
                Vector3.ProjectOnPlane(
                    Vector3.right,
                    forward);
        }

        if (right.sqrMagnitude <=
            0.000001f)
        {
            return fallbackRotation;
        }

        right.Normalize();

        Vector3 correctedUp =
            Vector3.Cross(
                forward,
                right).normalized;

        return Quaternion.LookRotation(
            forward,
            correctedUp);
    }

    public override bool TryGetPreview(
        CameraVolume volume,
        out CameraPreviewData preview)
    {
        preview = default;

        if (volume == null)
        {
            return false;
        }

        CameraVolume.CameraParams config =
            volume.Config;

        Vector3 origin;

        switch (_anchorMode)
        {
            case BlendAnchorMode.PlayerRelative:
                {
                    origin =
                        volume.GetGroundPosition(
                            out Vector3 groundPosition)
                            ? groundPosition
                            : volume.transform.position;

                    break;
                }

            case BlendAnchorMode.GroundRelative:
                {
                    if (!volume.GetGroundPosition(
                            out origin))
                    {
                        return false;
                    }

                    break;
                }

            default:
                return false;
        }

        Quaternion baseRotation =
            volume.transform.rotation;

        Vector3 cameraPosition =
            origin +
            baseRotation *
            config.offset;

        Quaternion cameraRotation =
            CreateLookRotation(
                cameraPosition,
                origin,
                baseRotation);

        preview = new CameraPreviewData
        {
            cameraPosition =
                cameraPosition,

            cameraRotation =
                cameraRotation,

            targetPosition =
                origin,

            targetRotation =
                baseRotation,

            fieldOfView =
                Mathf.Clamp(
                    config.fieldOfView,
                    1f,
                    179f)
        };

        return true;
    }

    private struct BlendSample
    {
        public Vector3 origin;
        public Vector3 offset;

        public Vector3 baseForward;
        public Vector3 baseUp;

        public Vector3 rotationLimitMin;
        public Vector3 rotationLimitMax;

        public float fieldOfView;
        public float smoothness;
        public float weight;
    }
}