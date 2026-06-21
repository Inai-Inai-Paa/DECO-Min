using UnityEngine;

[CreateAssetMenu(
    fileName = "CameraFollowState",
    menuName = "State/Camera/Follow")]
public sealed class CameraFollowState : CameraState
{
    [Header("Target Pivot")]

    [Tooltip(
        "プレイヤー座標から見たPivot位置。\n" +
        "この値はワールド軸基準のオフセットとして扱います。")]
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
    private bool _allowRoll = false;

    [Header("Time")]

    [Tooltip(
        "Time.timeScaleの影響を受けない時間を使用します。")]
    [SerializeField]
    private bool _useUnscaledTime = false;

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

        stateName = "Follow";
    }

    public override void Enter()
    {
        /*
         * State進入時にはCamera Transformを直接変更しない。
         *
         * 遷移前のCamera位置・回転・FOVを維持し、
         * Update内の補間によってFollow状態へ移行する。
         *
         * Orbit角度はCameraControllerが保持しているため、
         * CameraStateを跨いでも操作角度が維持される。
         */
    }

    public override void Exit()
    {
        /*
         * CameraControllerがOrbit角度を保持するため、
         * Exit時に回転情報を破棄しない。
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

        /*
         * フレームレート依存を抑えた補間率。
         *
         * Smoothnessが大きいほど速く目標へ近づく。
         * Smoothnessが小さいほど遷移がゆっくりになる。
         *
         * Smoothnessが0の場合は即時反映する。
         */
        float interpolationRate =
            smoothness <= Mathf.Epsilon
                ? 1f
                : 1f -
                  Mathf.Exp(
                      -smoothness *
                      deltaTime);

        /*
         * CameraControllerが保持している共通Orbit角度を更新する。
         *
         * X = Pitch
         * Y = Yaw
         * Z = Roll
         *
         * RotationLimitは現在のCameraVolumeから取得する。
         */
        Vector3 orbitAngles =
            _controller.UpdateOrbitAngles(
                config.rotationLimitMin,
                config.rotationLimitMax,
                smoothness,
                deltaTime);

        /*
         * CameraVolumeのTransform Rotationを基準姿勢として、
         * TPS用のOrbit回転を生成する。
         *
         * CreateCameraOrbitRotation内では、
         *
         * Yaw:
         *     ワールドY軸
         *
         * Pitch:
         *     Yaw適用後のカメラ右軸
         *
         * Roll:
         *     Pitch適用後のカメラ前方軸
         *
         * として計算される。
         */
        Quaternion orbitRotation =
            CameraController.CreateCameraOrbitRotation(
                _volume.transform.rotation,
                orbitAngles);

        /*
         * PivotOffsetはワールド座標として直接加算する。
         *
         * PlayerのTransform Rotationや
         * CameraVolumeのTransform Rotationの影響は受けない。
         */
        Vector3 pivotPosition =
            _target.position +
            _pivotOffset;

        /*
         * Config.offsetはOrbit姿勢に対する
         * ローカルカメラ位置として使用する。
         *
         * 例:
         *
         * (0, 0, -5)
         *     中央配置のTPSカメラ
         *
         * (0.7, 0, -5)
         *     右肩越しのTPSカメラ
         */
        Vector3 targetCameraPosition =
            pivotPosition +
            orbitRotation *
            config.offset;

        /*
         * カメラ姿勢へOrbit Quaternionを直接代入しない。
         *
         * Orbit回転はカメラ位置を決定するために使用し、
         * 実際のカメラ姿勢はPivotを見る回転として生成する。
         *
         * これにより、PitchやVolume基準姿勢の合成によって
         * 意図しないZ軸傾斜が発生することを防ぐ。
         */
        Quaternion targetCameraRotation =
            CreateStableLookRotation(
                targetCameraPosition,
                pivotPosition,
                _camera.transform.rotation);

        /*
         * Rollを許可している場合だけ、
         * カメラのローカル前方軸へZ回転を追加する。
         */
        if (_allowRoll)
        {
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

        /*
         * 位置をSmoothnessで補間する。
         *
         * FollowState進入時も、現在位置からTPS位置へ
         * 滑らかに移動する。
         */
        cameraTransform.position =
            Vector3.Lerp(
                cameraTransform.position,
                targetCameraPosition,
                interpolationRate);

        /*
         * 回転も同じSmoothnessで補間する。
         *
         * State進入時の急激な方向変化と、
         * 過去に残ったZ軸傾斜を滑らかに補正する。
         */
        cameraTransform.rotation =
            Quaternion.Slerp(
                cameraTransform.rotation,
                targetCameraRotation,
                interpolationRate);

        /*
         * Perspective Cameraの場合だけFOVを補間する。
         */
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
    /// CameraFollowStateのEditorプレビュー情報を生成する。
    /// </summary>
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

        /*
         * Edit Modeでは実際のPlayer位置を使用しない。
         *
         * Volume直下の地面を仮想Player位置として扱い、
         * 地面が見つからない場合はVolume自身の位置を使う。
         */
        Vector3 targetPosition =
            volume.GetGroundPosition(
                out Vector3 groundPosition)
                ? groundPosition
                : volume.transform.position;

        /*
         * PivotOffsetは実行時と同様に
         * ワールド座標として直接加算する。
         */
        Vector3 pivotPosition =
            targetPosition +
            _pivotOffset;

        /*
         * Editorプレビューではユーザー入力角度を0とする。
         *
         * CameraVolumeのTransform Rotationを
         * カメラ軌道の基準姿勢として使用する。
         */
        Quaternion orbitRotation =
            CameraController.CreateCameraOrbitRotation(
                volume.transform.rotation,
                Vector3.zero);

        Vector3 cameraPosition =
            pivotPosition +
            orbitRotation *
            config.offset;

        /*
         * Previewでも実行時と同じ方法で
         * Pivotを見るカメラ姿勢を生成する。
         *
         * これにより、Previewと実行時の向きが一致する。
         */
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

    /// <summary>
    /// カメラ位置から対象位置を見る、安定した回転を生成する。
    ///
    /// 通常時はワールドY軸を上方向として使用するため、
    /// 意図しないRollを防止できる。
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
            0.000001f)
        {
            return fallbackRotation;
        }

        forward.Normalize();

        float verticalDot =
            Mathf.Abs(
                Vector3.Dot(
                    forward,
                    Vector3.up));

        /*
         * 真上または真下に近くなければ、
         * ワールドY軸を上方向として使用する。
         */
        if (verticalDot < 0.999f)
        {
            return Quaternion.LookRotation(
                forward,
                Vector3.up);
        }

        /*
         * 真上または真下を向く場合は、
         * forwardとVector3.upがほぼ平行になるため、
         * LookRotationのRoll方向が不安定になる。
         *
         * 現在姿勢の右方向を投影して、
         * 安定したUp方向を再構築する。
         */
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
            right =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
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
                right);

        if (correctedUp.sqrMagnitude <=
            0.000001f)
        {
            return fallbackRotation;
        }

        correctedUp.Normalize();

        return Quaternion.LookRotation(
            forward,
            correctedUp);
    }
}