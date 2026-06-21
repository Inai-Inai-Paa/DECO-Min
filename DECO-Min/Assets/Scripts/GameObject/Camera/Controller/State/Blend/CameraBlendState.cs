
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CameraBlendState",
    menuName = "State/Camera/Blend")]
public sealed class CameraBlendState : CameraState
{
    private const float DirectionEpsilon =
        0.000001f;

    public enum BlendAnchorMode
    {
        PlayerRelative,
        GroundRelative
    }

    [Header("Blend Behavior")]

    [SerializeField]
    private BlendAnchorMode _anchorMode =
        BlendAnchorMode.PlayerRelative;

    [Header("Transition")]

    [Tooltip(
        "State進入時に現在のカメラ位置から" +
        "PitchとYawを逆算します。")]
    [SerializeField]
    private bool _initializeRotationOnEnter =
        true;

    [Header("Time")]

    [SerializeField]
    private bool _useUnscaledTime;

    /*
     * 毎フレームのGC Allocを避けるため、
     * BlendSample一覧を再利用する。
     */
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

    public override void Enter()
    {
        if (!_initializeRotationOnEnter ||
            _controller == null ||
            _camera == null ||
            _target == null)
        {
            return;
        }

        /*
         * 現在のPlayer位置から、
         * BlendState全体の基準情報を構築する。
         */
        if (!TryBuildBlendContext(
                _target.position,
                out BlendContext context))
        {
            return;
        }

        /*
         * 現在のカメラ位置から、
         * ブレンド後の基準姿勢に対応するOrbit角度を逆算する。
         */
        if (!CameraController
                .TryCalculateOrbitAnglesFromPosition(
                    _camera.transform.position,
                    context.origin,
                    context.baseRotation,
                    context.offset,
                    out Vector3 orbitAngles))
        {
            return;
        }

        /*
         * BlendStateでは意図しないZ傾斜を防ぐため、
         * Rollを使用しない。
         */
        orbitAngles.z =
            0f;

        /*
         * 現在角度はそのまま維持し、
         * 目標角度だけをブレンド後の制限範囲へ設定する。
         */
        _controller.InitializeOrbitAngles(
            orbitAngles,
            context.rotationLimitMin,
            context.rotationLimitMax);
    }

    public override void Exit()
    {
        /*
         * Orbit角度はCameraControllerが保持するため、
         * BlendState終了時にも破棄しない。
         */
    }

    public override void Update()
    {
        if (_controller == null ||
            _camera == null ||
            _target == null)
        {
            return;
        }

        float deltaTime =
            _useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        if (!TryBuildBlendContext(
                _target.position,
                out BlendContext context))
        {
            return;
        }

        /*
         * X = Pitch
         * Y = Yaw
         * Z = Roll
         */
        Vector3 orbitAngles =
            _controller.UpdateOrbitAngles(
                context.rotationLimitMin,
                context.rotationLimitMax,
                context.smoothness,
                deltaTime);

        /*
         * BlendStateではRollをカメラ位置・姿勢へ適用しない。
         */
        orbitAngles.z =
            0f;

        /*
         * ブレンド済み基準姿勢に対して、
         * Orbit回転を一度だけ適用する。
         *
         * VolumeごとにOrbit回転を適用してから
         * カメラ位置を平均すると、
         * マウスY入力がYawのように見える場合がある。
         */
        Quaternion orbitRotation =
            CameraController.CreateCameraOrbitRotation(
                context.baseRotation,
                orbitAngles);

        Vector3 targetCameraPosition =
            context.origin +
            orbitRotation *
            context.offset;

        ApplyCameraTransform(
            targetCameraPosition,
            context.origin,
            context.fieldOfView,
            context.smoothness,
            deltaTime);
    }

    /// <summary>
    /// 現在有効なBlend Volumeを収集し、
    /// 1つのBlendContextへまとめる。
    /// </summary>
    private bool TryBuildBlendContext(
        Vector3 playerPosition,
        out BlendContext context)
    {
        context =
            default;

        if (_controller == null)
        {
            return false;
        }

        IReadOnlyList<CameraVolume> volumes =
            _controller.Volumes;

        if (volumes == null ||
            volumes.Count == 0)
        {
            return false;
        }

        _blendSamples.Clear();

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

        if (totalWeight <=
            Mathf.Epsilon)
        {
            return false;
        }

        Vector3 blendedForward =
            weightedForward /
            totalWeight;

        Vector3 blendedUp =
            weightedUp /
            totalWeight;

        context =
            new BlendContext
            {
                origin =
                    weightedOrigin /
                    totalWeight,

                offset =
                    weightedOffset /
                    totalWeight,

                baseRotation =
                    CreateBlendedBaseRotation(
                        blendedForward,
                        blendedUp),

                rotationLimitMin =
                    weightedRotationLimitMin /
                    totalWeight,

                rotationLimitMax =
                    weightedRotationLimitMax /
                    totalWeight,

                fieldOfView =
                    weightedFieldOfView /
                    totalWeight,

                smoothness =
                    weightedSmoothness /
                    totalWeight
            };

        return true;
    }

    /// <summary>
    /// 1つのCameraVolumeからブレンド情報を取得する。
    /// </summary>
    private bool TryGetBlendSample(
        CameraVolume volume,
        Vector3 playerPosition,
        out BlendSample sample)
    {
        sample =
            default;

        if (volume == null ||
            !volume.isActiveAndEnabled)
        {
            return false;
        }

        CameraBlendState blendState =
            volume.RuntimeState
                as CameraBlendState;

        /*
         * CameraBlendStateを持つVolumeだけを対象にする。
         */
        if (blendState == null)
        {
            return false;
        }

        float radius =
            volume.Radius;

        if (radius <=
            Mathf.Epsilon)
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

        /*
         * Volume中心で1、外周で0となるWeight。
         */
        float weight =
            1f -
            distance /
            radius;

        if (weight <=
            Mathf.Epsilon)
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

        sample =
            new BlendSample
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
    /// ForwardとUpの加重平均から、
    /// 直交した基準姿勢を生成する。
    /// </summary>
    private static Quaternion CreateBlendedBaseRotation(
        Vector3 blendedForward,
        Vector3 blendedUp)
    {
        if (blendedForward.sqrMagnitude <=
            DirectionEpsilon)
        {
            blendedForward =
                Vector3.forward;
        }

        blendedForward.Normalize();

        /*
         * UpからForward方向成分を除外し、
         * ForwardとUpを直交させる。
         */
        Vector3 correctedUp =
            Vector3.ProjectOnPlane(
                blendedUp,
                blendedForward);

        if (correctedUp.sqrMagnitude <=
            DirectionEpsilon)
        {
            correctedUp =
                Vector3.ProjectOnPlane(
                    Vector3.up,
                    blendedForward);
        }

        if (correctedUp.sqrMagnitude <=
            DirectionEpsilon)
        {
            correctedUp =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
                    blendedForward);
        }

        if (correctedUp.sqrMagnitude <=
            DirectionEpsilon)
        {
            return Quaternion.identity;
        }

        correctedUp.Normalize();

        return Quaternion.LookRotation(
            blendedForward,
            correctedUp);
    }

    /// <summary>
    /// 計算済みのカメラ位置・注視点をCameraへ反映する。
    /// </summary>
    private void ApplyCameraTransform(
        Vector3 targetCameraPosition,
        Vector3 targetLookPosition,
        float targetFieldOfView,
        float smoothness,
        float deltaTime)
    {
        float interpolationRate =
            CalculateInterpolationRate(
                smoothness,
                deltaTime);

        Transform cameraTransform =
            _camera.transform;

        /*
         * 位置をSmoothnessで補間する。
         */
        cameraTransform.position =
            Vector3.Lerp(
                cameraTransform.position,
                targetCameraPosition,
                interpolationRate);

        /*
         * 補間後のカメラ位置から、
         * Blend Originを見る姿勢を作る。
         */
        Quaternion targetRotation =
            CreateStableLookRotation(
                cameraTransform.position,
                targetLookPosition,
                cameraTransform.rotation);

        cameraTransform.rotation =
            Quaternion.Slerp(
                cameraTransform.rotation,
                targetRotation,
                interpolationRate);

        if (!_camera.orthographic)
        {
            _camera.fieldOfView =
                Mathf.Lerp(
                    _camera.fieldOfView,
                    targetFieldOfView,
                    interpolationRate);
        }
    }

    /// <summary>
    /// ワールドYを上方向として、
    /// 意図しないRollを防いだLookRotationを生成する。
    /// </summary>
    private static Quaternion CreateStableLookRotation(
        Vector3 cameraPosition,
        Vector3 targetPosition,
        Quaternion fallbackRotation)
    {
        Vector3 forward =
            targetPosition -
            cameraPosition;

        if (forward.sqrMagnitude <=
            DirectionEpsilon)
        {
            return fallbackRotation;
        }

        forward.Normalize();

        float verticalDot =
            Mathf.Abs(
                Vector3.Dot(
                    forward,
                    Vector3.up));

        if (verticalDot < 0.999f)
        {
            return Quaternion.LookRotation(
                forward,
                Vector3.up);
        }

        /*
         * 真上・真下付近では現在姿勢のRightを投影し、
         * Up方向を再構築する。
         */
        Vector3 right =
            fallbackRotation *
            Vector3.right;

        right =
            Vector3.ProjectOnPlane(
                right,
                forward);

        if (right.sqrMagnitude <=
            DirectionEpsilon)
        {
            right =
                Vector3.ProjectOnPlane(
                    Vector3.right,
                    forward);
        }

        if (right.sqrMagnitude <=
            DirectionEpsilon)
        {
            right =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
                    forward);
        }

        if (right.sqrMagnitude <=
            DirectionEpsilon)
        {
            return fallbackRotation;
        }

        right.Normalize();

        Vector3 correctedUp =
            Vector3.Cross(
                forward,
                right);

        if (correctedUp.sqrMagnitude <=
            DirectionEpsilon)
        {
            return fallbackRotation;
        }

        correctedUp.Normalize();

        return Quaternion.LookRotation(
            forward,
            correctedUp);
    }

    private static float CalculateInterpolationRate(
        float smoothness,
        float deltaTime)
    {
        if (smoothness <=
            Mathf.Epsilon)
        {
            return 1f;
        }

        return
            1f -
            Mathf.Exp(
                -smoothness *
                deltaTime);
    }

    /// <summary>
    /// CameraBlendStateのEditorプレビューを生成する。
    /// </summary>
    public override bool TryGetPreview(
        CameraVolume volume,
        out CameraPreviewData preview)
    {
        preview =
            default;

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
            CreateStableLookRotation(
                cameraPosition,
                origin,
                baseRotation);

        preview =
            new CameraPreviewData
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

    private struct BlendContext
    {
        public Vector3 origin;
        public Vector3 offset;

        public Quaternion baseRotation;

        public Vector3 rotationLimitMin;
        public Vector3 rotationLimitMax;

        public float fieldOfView;
        public float smoothness;
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
