using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CameraBlendState",
    menuName = "State/Camera/Blend")]
public sealed class CameraBlendState : CameraState
{
    private const float DirectionEpsilon =
        0.000001f;

    private const float RestartTransitionProgress =
        0.2f;

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

    private readonly List<BlendSample> _blendSamples =
        new List<BlendSample>();

    /*
     * 0 = 進入直後
     * 1 = 通常Smoothnessへ完全復帰
     */
    private float _enterTransitionProgress;

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
        _enterTransitionProgress =
            0f;

        if (!_initializeRotationOnEnter ||
            _controller == null ||
            _camera == null ||
            _target == null)
        {
            return;
        }

        if (!TryBuildBlendContext(
                _target.position,
                out BlendContext context))
        {
            return;
        }

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

        orbitAngles.z =
            0f;

        _controller.InitializeOrbitAngles(
            orbitAngles,
            context.rotationLimitMin,
            context.rotationLimitMax);
    }

    public override void Exit()
    {
    }

    /// <summary>
    /// Blend Volume同士で主要Volumeが変化した際に、
    /// 進入Smoothnessを再度少し弱める。
    /// </summary>
    public void RestartEnterTransition()
    {
        _enterTransitionProgress =
            Mathf.Min(
                _enterTransitionProgress,
                RestartTransitionProgress);
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

        if (deltaTime <=
            0f)
        {
            return;
        }

        if (!TryBuildBlendContext(
                _target.position,
                out BlendContext context))
        {
            return;
        }

        Vector3 orbitAngles =
            _controller.UpdateOrbitAngles(
                context.rotationLimitMin,
                context.rotationLimitMax,
                deltaTime);

        orbitAngles.z =
            0f;

        Quaternion orbitRotation =
            CameraController.CreateCameraOrbitRotation(
                context.baseRotation,
                orbitAngles);

        Vector3 targetCameraPosition =
            context.origin +
            orbitRotation *
            context.offset;

        GetEffectiveSmoothness(
            context,
            deltaTime,
            out float smoothnessPosition,
            out float transitionSmoothnessTarget);

        float effectiveSmoothnessTarget =
            _controller.HasOrbitInputThisFrame
                ? Mathf.Max(
                    0.01f,
                    context.smoothnessTarget)
                : transitionSmoothnessTarget;

        ApplyCameraTransform(
            targetCameraPosition,
            context.origin,
            context.fieldOfView,
            smoothnessPosition,
            effectiveSmoothnessTarget,
            deltaTime);
    }

    private void GetEffectiveSmoothness(
        BlendContext context,
        float deltaTime,
        out float smoothnessPosition,
        out float smoothnessTarget)
    {
        float recoveryDuration =
            Mathf.Max(
                0f,
                context.enterSmoothnessRecoveryDuration);

        if (recoveryDuration <=
            Mathf.Epsilon)
        {
            _enterTransitionProgress =
                1f;
        }
        else
        {
            _enterTransitionProgress =
                Mathf.MoveTowards(
                    _enterTransitionProgress,
                    1f,
                    deltaTime /
                    recoveryDuration);
        }

        float transitionRate =
            Mathf.SmoothStep(
                0f,
                1f,
                _enterTransitionProgress);

        smoothnessPosition =
            Mathf.Lerp(
                Mathf.Max(
                    0.01f,
                    context.enterSmoothnessPosition),
                Mathf.Max(
                    0.01f,
                    context.smoothnessPosition),
                transitionRate);

        smoothnessTarget =
            Mathf.Lerp(
                Mathf.Max(
                    0.01f,
                    context.enterSmoothnessTarget),
                Mathf.Max(
                    0.01f,
                    context.smoothnessTarget),
                transitionRate);
    }

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

        float weightedSmoothnessPosition =
            0f;

        float weightedSmoothnessTarget =
            0f;

        float weightedEnterSmoothnessPosition =
            0f;

        float weightedEnterSmoothnessTarget =
            0f;

        float weightedEnterSmoothnessRecoveryDuration =
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

            weightedSmoothnessPosition +=
                sample.smoothnessPosition *
                sample.weight;

            weightedSmoothnessTarget +=
                sample.smoothnessTarget *
                sample.weight;

            weightedEnterSmoothnessPosition +=
                sample.enterSmoothnessPosition *
                sample.weight;

            weightedEnterSmoothnessTarget +=
                sample.enterSmoothnessTarget *
                sample.weight;

            weightedEnterSmoothnessRecoveryDuration +=
                sample.enterSmoothnessRecoveryDuration *
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

                smoothnessPosition =
                    weightedSmoothnessPosition /
                    totalWeight,

                smoothnessTarget =
                    weightedSmoothnessTarget /
                    totalWeight,

                enterSmoothnessPosition =
                    weightedEnterSmoothnessPosition /
                    totalWeight,

                enterSmoothnessTarget =
                    weightedEnterSmoothnessTarget /
                    totalWeight,

                enterSmoothnessRecoveryDuration =
                    weightedEnterSmoothnessRecoveryDuration /
                    totalWeight
            };

        return true;
    }

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
            radius *
            radius)
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

                smoothnessPosition =
                    Mathf.Max(
                        0.01f,
                        config.smoothnessPosition),

                smoothnessTarget =
                    Mathf.Max(
                        0.01f,
                        config.smoothnessTarget),

                enterSmoothnessPosition =
                    Mathf.Max(
                        0.01f,
                        config.enterSmoothnessPosition),

                enterSmoothnessTarget =
                    Mathf.Max(
                        0.01f,
                        config.enterSmoothnessTarget),

                enterSmoothnessRecoveryDuration =
                    Mathf.Max(
                        0f,
                        config.enterSmoothnessRecoveryDuration),

                weight =
                    weight
            };

        return true;
    }

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

    private void ApplyCameraTransform(
        Vector3 targetCameraPosition,
        Vector3 targetLookPosition,
        float targetFieldOfView,
        float smoothnessPosition,
        float smoothnessTarget,
        float deltaTime)
    {
        float positionInterpolationRate =
            CalculateInterpolationRate(
                smoothnessPosition,
                deltaTime);

        float targetInterpolationRate =
            CalculateInterpolationRate(
                smoothnessTarget,
                deltaTime);

        Transform cameraTransform =
            _camera.transform;

        cameraTransform.position =
            Vector3.Lerp(
                cameraTransform.position,
                targetCameraPosition,
                positionInterpolationRate);

        Quaternion targetRotation =
            CreateStableLookRotation(
                cameraTransform.position,
                targetLookPosition,
                cameraTransform.rotation);

        cameraTransform.rotation =
            Quaternion.Slerp(
                cameraTransform.rotation,
                targetRotation,
                targetInterpolationRate);

        if (!_camera.orthographic)
        {
            _camera.fieldOfView =
                Mathf.Lerp(
                    _camera.fieldOfView,
                    targetFieldOfView,
                    positionInterpolationRate);
        }
    }

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

        if (verticalDot <
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

        public float smoothnessPosition;
        public float smoothnessTarget;

        public float enterSmoothnessPosition;
        public float enterSmoothnessTarget;
        public float enterSmoothnessRecoveryDuration;
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

        public float smoothnessPosition;
        public float smoothnessTarget;

        public float enterSmoothnessPosition;
        public float enterSmoothnessTarget;
        public float enterSmoothnessRecoveryDuration;

        public float weight;
    }
}
