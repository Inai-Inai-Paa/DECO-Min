using UnityEngine;

[CreateAssetMenu(
    fileName = "CameraFollowState",
    menuName = "State/Camera/Follow")]
public sealed class CameraFollowState : CameraState
{
    private const float DirectionEpsilon =
        0.000001f;

    [Header("Target Pivot")]

    [Tooltip(
        "プレイヤー座標から見たPivot位置。\n" +
        "ワールド軸基準のオフセットとして扱います。")]
    [SerializeField]
    private Vector3 _pivotOffset =
        new Vector3(
            0f,
            1.5f,
            0f);

    [Header("Rotation")]

    [Tooltip(
        "Z軸のRoll操作をカメラ姿勢へ反映します。\n" +
        "通常のTPSカメラでは無効を推奨します。")]
    [SerializeField]
    private bool _allowRoll;

    [Header("Transition")]

    [Tooltip(
        "State進入時に現在のカメラ位置から" +
        "PitchとYawを逆算します。")]
    [SerializeField]
    private bool _initializeRotationOnEnter =
        true;

    [Header("Time")]

    [Tooltip(
        "Time.timeScaleの影響を受けない時間を使用します。")]
    [SerializeField]
    private bool _useUnscaledTime;

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
            "Follow";
    }

    public override void Enter()
    {
        if (!_initializeRotationOnEnter ||
            _controller == null ||
            _camera == null ||
            _target == null ||
            _volume == null)
        {
            return;
        }

        CameraVolume.CameraParams config =
            _volume.Config;

        Vector3 pivotPosition =
            GetPivotPosition();

        /*
         * 現在のカメラ位置から、
         * FollowStateに対応するOrbit角度を逆算する。
         */
        if (!CameraController
                .TryCalculateOrbitAnglesFromPosition(
                    _camera.transform.position,
                    pivotPosition,
                    _volume.transform.rotation,
                    config.offset,
                    out Vector3 orbitAngles))
        {
            return;
        }

        if (_allowRoll)
        {
            orbitAngles.z =
                CalculateCurrentCameraRoll(
                    _camera.transform,
                    pivotPosition);
        }
        else
        {
            orbitAngles.z =
                0f;
        }

        /*
         * 現在角度は逆算値を維持し、
         * 目標角度だけをVolumeの制限範囲へ設定する。
         */
        _controller.InitializeOrbitAngles(
            orbitAngles,
            config.rotationLimitMin,
            config.rotationLimitMax);
    }

    public override void Exit()
    {
        /*
         * Orbit角度はControllerが保持するため破棄しない。
         */
    }

    public override void Update()
    {
        if (_controller == null ||
            _camera == null ||
            _target == null ||
            _volume == null)
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

        CameraVolume.CameraParams config =
            _volume.Config;

        float smoothness =
            Mathf.Max(
                0f,
                config.smoothness);

        float interpolationRate =
            CalculateInterpolationRate(
                smoothness,
                deltaTime);

        /*
         * X = Pitch
         * Y = Yaw
         * Z = Roll
         */
        Vector3 orbitAngles =
            _controller.UpdateOrbitAngles(
                config.rotationLimitMin,
                config.rotationLimitMax,
                smoothness,
                deltaTime);

        /*
         * Rollを許可しない場合は、
         * カメラ軌道位置にもZ回転を適用しない。
         */
        if (!_allowRoll)
        {
            orbitAngles.z =
                0f;
        }

        Quaternion orbitRotation =
            CameraController.CreateCameraOrbitRotation(
                _volume.transform.rotation,
                orbitAngles);

        Vector3 pivotPosition =
            GetPivotPosition();

        /*
         * Orbit回転はカメラ位置の計算に使う。
         */
        Vector3 targetCameraPosition =
            pivotPosition +
            orbitRotation *
            config.offset;

        /*
         * カメラ姿勢はPivotを見る回転として作る。
         *
         * Orbit Quaternionをそのまま代入しないことで、
         * 意図しないZ軸傾斜を抑制する。
         */
        Quaternion targetCameraRotation =
            CreateStableLookRotation(
                targetCameraPosition,
                pivotPosition,
                _camera.transform.rotation);

        if (_allowRoll)
        {
            /*
             * Rollを許可する場合のみ、
             * カメラローカル前方軸へZ回転を加える。
             */
            targetCameraRotation *=
                Quaternion.AngleAxis(
                    orbitAngles.z,
                    Vector3.forward);
        }

        float targetFieldOfView =
            Mathf.Clamp(
                config.fieldOfView,
                1f,
                179f);

        Transform cameraTransform =
            _camera.transform;

        cameraTransform.position =
            Vector3.Lerp(
                cameraTransform.position,
                targetCameraPosition,
                interpolationRate);

        cameraTransform.rotation =
            Quaternion.Slerp(
                cameraTransform.rotation,
                targetCameraRotation,
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
    /// 現在のPlayer位置からPivot位置を取得する。
    /// </summary>
    private Vector3 GetPivotPosition()
    {
        return
            _target.position +
            _pivotOffset;
    }

    /// <summary>
    /// 現在のCamera TransformからRoll角度を取得する。
    /// </summary>
    private static float CalculateCurrentCameraRoll(
        Transform cameraTransform,
        Vector3 pivotPosition)
    {
        Vector3 forward =
            pivotPosition -
            cameraTransform.position;

        if (forward.sqrMagnitude <=
            DirectionEpsilon)
        {
            return 0f;
        }

        forward.Normalize();

        /*
         * 真上・真下付近ではRollを安定して取得できない。
         */
        if (Mathf.Abs(
                Vector3.Dot(
                    forward,
                    Vector3.up)) >
            0.999f)
        {
            return 0f;
        }

        Quaternion noRollRotation =
            Quaternion.LookRotation(
                forward,
                Vector3.up);

        Vector3 noRollUp =
            noRollRotation *
            Vector3.up;

        Vector3 currentUp =
            Vector3.ProjectOnPlane(
                cameraTransform.up,
                forward);

        if (currentUp.sqrMagnitude <=
            DirectionEpsilon)
        {
            return 0f;
        }

        currentUp.Normalize();

        return NormalizeSignedAngle(
            Vector3.SignedAngle(
                noRollUp,
                currentUp,
                forward));
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

    /// <summary>
    /// CameraFollowStateのEditorプレビューを生成する。
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

        Vector3 targetPosition =
            volume.GetGroundPosition(
                out Vector3 groundPosition)
                ? groundPosition
                : volume.transform.position;

        Vector3 pivotPosition =
            targetPosition +
            _pivotOffset;

        Quaternion orbitRotation =
            CameraController.CreateCameraOrbitRotation(
                volume.transform.rotation,
                Vector3.zero);

        Vector3 cameraPosition =
            pivotPosition +
            orbitRotation *
            config.offset;

        Quaternion cameraRotation =
            CreateStableLookRotation(
                cameraPosition,
                pivotPosition,
                volume.transform.rotation);

        preview =
            new CameraPreviewData
            {
                cameraPosition =
                    cameraPosition,

                cameraRotation =
                    cameraRotation,

                targetPosition =
                    pivotPosition,

                targetRotation =
                    volume.transform.rotation,

                fieldOfView =
                    Mathf.Clamp(
                        config.fieldOfView,
                        1f,
                        179f)
            };

        return true;
    }

    private static float NormalizeSignedAngle(
        float angle)
    {
        return
            Mathf.Repeat(
                angle + 180f,
                360f) - 180f;
    }
}

