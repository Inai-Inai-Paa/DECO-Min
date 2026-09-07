using UnityEngine;

public abstract class CameraState : State
{
    // カメラ制御全体を管理するController
    protected CameraController _controller;

    // 操作対象のCamera
    protected Camera _camera;

    // Stateの切り替えを管理するStateMachine
    protected StateMachine _stateMachine;

    // カメラが追従する対象
    protected Transform _target;

    // このStateを所有しているCameraVolume
    protected CameraVolume _volume;
    private float _enterTransitionElapsed;
    /// <summary>
    /// CameraStateの実行時初期化。
    /// </summary>
    public virtual void Initialize(
        CameraController controller,
        Camera camera,
        StateMachine stateMachine,
        Transform target,
        CameraVolume volume)
    {
        _controller =
            controller;

        _camera =
            camera;

        _stateMachine =
            stateMachine;

        _target =
            target;

        _volume =
            volume;
    }

    /// <summary>
    /// Editor上で表示する標準プレビューを計算する。
    ///
    /// CameraVolumeのTransform回転を基準姿勢として扱い、
    /// CameraVolume.Config.offsetをローカルオフセットとして適用する。
    /// </summary>
    public virtual bool TryGetPreview(
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

        /*
         * 地面が見つかった場合はVolume直下の地面、
         * 見つからない場合はVolume自身の座標をOriginとする。
         */
        Vector3 origin =
            volume.GetGroundPosition(
                out Vector3 groundPosition)
                ? groundPosition
                : volume.transform.position + config.targetOffset;

        /*
         * CameraVolumeのTransform回転を、
         * カメラ配置の基準姿勢として使用する。
         */
        Quaternion baseRotation =
            volume.transform.rotation;

        /*
         * Config.offsetはCameraVolume基準の
         * ローカルオフセットとして扱う。
         */
        Vector3 cameraPosition =
            origin +
            baseRotation *
            config.cameraOffset;

        Quaternion cameraRotation =
            CreateLookRotation(
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

    /// <summary>
    /// カメラ位置から対象位置を向く回転を生成する。
    ///
    /// 真上や真下を向く場合のLookRotation不安定化も回避する。
    /// </summary>
    protected static Quaternion CreateLookRotation(
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

        /*
         * forwardとVector3.upがほぼ平行な場合は、
         * Vector3.forwardを上方向として使用する。
         */
        Vector3 up =
            Mathf.Abs(
                Vector3.Dot(
                    forward,
                    Vector3.up)) > 0.999f
                ? Vector3.forward
                : Vector3.up;

        return Quaternion.LookRotation(
            forward,
            up);
    }
}