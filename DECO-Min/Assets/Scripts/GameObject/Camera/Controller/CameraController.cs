using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(100)]
public sealed class CameraController : MonoBehaviour
{
    private const float MinimumAngle =
        -180f;

    private const float MaximumAngle =
        180f;

    private const float FreeAxisTolerance =
        0.001f;

    [Header("References")]

    [SerializeField]
    private Player _player;

    [SerializeField]
    private Camera _controlledCamera;

    [Header("Default State")]

    [Tooltip(
        "どのCameraVolumeにも入っていない場合に使用するState")]
    [SerializeField]
    private CameraState _defaultStateTemplate;

    [Header("Camera Rotation Input")]

    [Tooltip("XはYaw感度、YはPitch感度")]
    [SerializeField]
    private Vector2 _mouseSensitivity =
        new Vector2(
            4f,
            4f);

    [SerializeField]
    private bool _invertVertical;

    [Tooltip("Q/EによるRoll回転速度")]
    [SerializeField]
    private float _rollSpeed =
        60f;

    private readonly List<CameraVolume> _volumes =
        new List<CameraVolume>();

    private StateMachine _stateMachine;

    private CameraState _defaultRuntimeState;
    private CameraVolume _currentVolume;

    /*
     * CameraStateを跨いで維持される操作回転。
     * Volumeの回転に対する相対角度として使用する。
     */
    private Vector3 _currentOrbitRotation;
    private Vector3 _targetOrbitRotation;

    public Camera ControlledCamera =>
        _controlledCamera;

    public StateMachine StateMachine =>
        _stateMachine;

    public Transform Target =>
        _player != null
            ? _player.transform
            : null;

    public CameraVolume CurrentVolume =>
        _currentVolume;

    public IReadOnlyList<CameraVolume> Volumes =>
        _volumes;

    public Vector3 CurrentOrbitRotation =>
        _currentOrbitRotation;

    private void Awake()
    {
        ResolveReferences();

        if (_player == null)
        {
            Debug.LogError(
                $"{nameof(CameraController)}: " +
                "Playerが設定されていません。",
                this);

            enabled =
                false;

            return;
        }

        if (_controlledCamera == null)
        {
            Debug.LogError(
                $"{nameof(CameraController)}: " +
                "Cameraが設定されていません。",
                this);

            enabled =
                false;

            return;
        }

        _stateMachine =
            new StateMachine();

        /*
         * OrbitRotationはVolume回転に対する相対角度。
         * 初期値は回転なしとする。
         */
        SetOrbitRotation(
            Vector3.zero);

        CreateDefaultState();
        InitializeVolumes();

        _stateMachine.ChangeState(
            _defaultRuntimeState);

        /*
         * 初期位置ですでにVolume内にいる場合にも対応する。
         */
        UpdateCurrentVolume();
    }

    private void FixedUpdate()
    {
        _stateMachine?.FixedUpdate();
    }

    private void LateUpdate()
    {
        /*
         * プレイヤー移動後にVolumeを選択し、
         * 選択済みStateを更新する。
         */
        UpdateCurrentVolume();

        _stateMachine?.Update();
    }

    /// <summary>
    /// 現在のプレイヤー位置から有効なVolumeを選択し、
    /// 必要な場合だけCameraStateを切り替える。
    /// </summary>
    private void UpdateCurrentVolume()
    {
        if (_player == null ||
            _stateMachine == null)
        {
            return;
        }

        Vector3 targetPosition =
            _player.transform.position;

        CameraVolume selectedVolume =
            SelectVolume(
                targetPosition);

        if (_currentVolume == selectedVolume)
        {
            return;
        }

        CameraState currentState =
            _stateMachine.GetState<CameraState>();

        CameraState nextState =
            selectedVolume != null
                ? selectedVolume.RuntimeState
                : _defaultRuntimeState;

        /*
         * BlendStateはControllerの全Volumeを参照する。
         *
         * Blend Volume同士の選択が変化しただけなら、
         * Stateインスタンスを切り替える必要はない。
         */
        bool keepCurrentBlendState =
            currentState is CameraBlendState &&
            nextState is CameraBlendState;

        _currentVolume =
            selectedVolume;

        if (keepCurrentBlendState)
        {
            return;
        }

        _stateMachine.ChangeState(
            nextState);
    }

    /// <summary>
    /// 指定座標を含むVolumeから、
    /// 現在使用するVolumeを選択する。
    /// </summary>
    private CameraVolume SelectVolume(
        Vector3 targetPosition)
    {
        CameraVolume selectedVolume =
            null;

        float selectedSquaredDistance =
            float.MaxValue;

        for (int i = 0;
             i < _volumes.Count;
             ++i)
        {
            CameraVolume volume =
                _volumes[i];

            if (volume == null ||
                !volume.isActiveAndEnabled ||
                volume.RuntimeState == null)
            {
                continue;
            }

            if (!volume.Contains(
                    targetPosition))
            {
                continue;
            }

            float squaredDistance =
                volume.GetSquaredDistance(
                    targetPosition);

            if (selectedVolume == null)
            {
                selectedVolume =
                    volume;

                selectedSquaredDistance =
                    squaredDistance;

                continue;
            }

            if (volume.Priority >
                selectedVolume.Priority)
            {
                selectedVolume =
                    volume;

                selectedSquaredDistance =
                    squaredDistance;

                continue;
            }

            /*
             * Priorityが同じ場合は、
             * Volume中心へ近いものを選択する。
             */
            if (volume.Priority ==
                    selectedVolume.Priority &&
                squaredDistance <
                    selectedSquaredDistance)
            {
                selectedVolume =
                    volume;

                selectedSquaredDistance =
                    squaredDistance;
            }
        }

        return selectedVolume;
    }

    /// <summary>
    /// 入力を反映し、制限と補間を適用したOrbit回転を返す。
    ///
    /// RotationLimitはVolumeまたはBlendStateから渡される。
    /// </summary>
    /// <summary>
    /// 入力を反映し、制限と補間を適用したOrbit角度を返す。
    ///
    /// X = Pitch
    /// Y = Yaw
    /// Z = Roll
    /// </summary>
    public Vector3 UpdateOrbitAngles(
        Vector3 rotationLimitMin,
        Vector3 rotationLimitMax,
        float smoothness,
        float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return _currentOrbitRotation;
        }

        Vector2 mouseDelta =
            ReadMouseDelta();

        float pitchDirection =
            _invertVertical
                ? 1f
                : -1f;

        // マウスYは必ずPitchへ加算する
        _targetOrbitRotation.x +=
            mouseDelta.y *
            _mouseSensitivity.y *
            pitchDirection;

        // マウスXは必ずYawへ加算する
        _targetOrbitRotation.y +=
            mouseDelta.x *
            _mouseSensitivity.x;

        _targetOrbitRotation.z +=
            ReadRollInput() *
            _rollSpeed *
            deltaTime;

        _targetOrbitRotation =
            ConstrainRotation(
                _targetOrbitRotation,
                rotationLimitMin,
                rotationLimitMax);

        float interpolationRate =
            smoothness <= Mathf.Epsilon
                ? 1f
                : 1f -
                  Mathf.Exp(
                      -smoothness *
                      deltaTime);

        _currentOrbitRotation =
            InterpolateRotation(
                _currentOrbitRotation,
                _targetOrbitRotation,
                rotationLimitMin,
                rotationLimitMax,
                interpolationRate);

        return _currentOrbitRotation;
    }

    /// <summary>
    /// TPS用の安定した回転を生成する。
    ///
    /// YawはワールドY軸、
    /// PitchはYaw適用後の右軸、
    /// Rollは最終的な前方軸へ適用する。
    /// </summary>
    public static Quaternion CreateCameraOrbitRotation(
        Quaternion baseRotation,
        Vector3 orbitAngles)
    {
        // Yawは常にワールド上方向
        Quaternion yawRotation =
            Quaternion.AngleAxis(
                orbitAngles.y,
                Vector3.up);

        Quaternion yawedRotation =
            yawRotation *
            baseRotation;

        // PitchはYaw後のカメラ右方向
        Vector3 pitchAxis =
            yawedRotation *
            Vector3.right;

        Quaternion pitchRotation =
            Quaternion.AngleAxis(
                orbitAngles.x,
                pitchAxis.normalized);

        Quaternion pitchedRotation =
            pitchRotation *
            yawedRotation;

        // Rollは最終的な前方方向
        Vector3 rollAxis =
            pitchedRotation *
            Vector3.forward;

        Quaternion rollRotation =
            Quaternion.AngleAxis(
                orbitAngles.z,
                rollAxis.normalized);

        return
            rollRotation *
            pitchedRotation;
    }

    public void SetOrbitRotation(
        Vector3 rotation)
    {
        _currentOrbitRotation =
            rotation;

        _targetOrbitRotation =
            rotation;
    }

    /// <summary>
    /// 実行中に追加・削除されたVolumeを再取得する。
    /// </summary>
    public void RefreshVolumes()
    {
        _currentVolume =
            null;

        _stateMachine?.ChangeState(
            _defaultRuntimeState);

        for (int i = 0;
             i < _volumes.Count;
             ++i)
        {
            CameraVolume volume =
                _volumes[i];

            if (volume != null)
            {
                volume.ReleaseRuntimeState();
            }
        }

        InitializeVolumes();
        UpdateCurrentVolume();
    }

    private void InitializeVolumes()
    {
        _volumes.Clear();

        CameraVolume[] foundVolumes =
            Object.FindObjectsByType<CameraVolume>(
                FindObjectsSortMode.None);

        for (int i = 0;
             i < foundVolumes.Length;
             ++i)
        {
            CameraVolume volume =
                foundVolumes[i];

            if (volume == null)
            {
                continue;
            }

            _volumes.Add(
                volume);

            volume.Initialize(
                this);
        }
    }

    private void CreateDefaultState()
    {
        if (_defaultStateTemplate == null)
        {
            return;
        }

        _defaultRuntimeState =
            Instantiate(
                _defaultStateTemplate);

        _defaultRuntimeState.name =
            $"{_defaultStateTemplate.name} Runtime Default";

        _defaultRuntimeState.hideFlags =
            HideFlags.HideAndDontSave;

        _defaultRuntimeState.Initialize(
            this,
            _controlledCamera,
            _stateMachine,
            _player.transform,
            null);
    }

    private void ResolveReferences()
    {
        if (_controlledCamera == null)
        {
            _controlledCamera =
                GetComponent<Camera>();
        }

        if (_controlledCamera == null)
        {
            _controlledCamera =
                Camera.main;
        }

        if (_player == null)
        {
            _player =
                Object.FindFirstObjectByType<Player>();
        }
    }

    private static Vector3 ConstrainRotation(
        Vector3 rotation,
        Vector3 minimum,
        Vector3 maximum)
    {
        rotation.x =
            ConstrainAxis(
                rotation.x,
                minimum.x,
                maximum.x);

        rotation.y =
            ConstrainAxis(
                rotation.y,
                minimum.y,
                maximum.y);

        rotation.z =
            ConstrainAxis(
                rotation.z,
                minimum.z,
                maximum.z);

        return rotation;
    }

    private static Vector3 InterpolateRotation(
        Vector3 current,
        Vector3 target,
        Vector3 minimum,
        Vector3 maximum,
        float interpolationRate)
    {
        return new Vector3(
            InterpolateAxis(
                current.x,
                target.x,
                minimum.x,
                maximum.x,
                interpolationRate),

            InterpolateAxis(
                current.y,
                target.y,
                minimum.y,
                maximum.y,
                interpolationRate),

            InterpolateAxis(
                current.z,
                target.z,
                minimum.z,
                maximum.z,
                interpolationRate));
    }

    private static float ConstrainAxis(
        float angle,
        float minimum,
        float maximum)
    {
        if (IsFreeAxis(
                minimum,
                maximum))
        {
            /*
             * 完全自由軸では角度を正規化しない。
             *
             * 180度を超えた際の不連続を防ぐ。
             */
            return angle;
        }

        float normalizedAngle =
            NormalizeSignedAngle(
                angle);

        return Mathf.Clamp(
            normalizedAngle,
            minimum,
            maximum);
    }

    private static float InterpolateAxis(
        float current,
        float target,
        float minimum,
        float maximum,
        float interpolationRate)
    {
        if (IsFreeAxis(
                minimum,
                maximum))
        {
            return Mathf.Lerp(
                current,
                target,
                interpolationRate);
        }

        /*
         * 自由回転から制限回転へ移行した場合は、
         * 720度などの連続角を等価な-180～180へ戻す。
         */
        current =
            NormalizeSignedAngle(
                current);

        target =
            Mathf.Clamp(
                NormalizeSignedAngle(
                    target),
                minimum,
                maximum);

        return Mathf.Lerp(
            current,
            target,
            interpolationRate);
    }

    private static bool IsFreeAxis(
        float minimum,
        float maximum)
    {
        return
            minimum <=
                MinimumAngle +
                FreeAxisTolerance &&
            maximum >=
                MaximumAngle -
                FreeAxisTolerance;
    }

    private static float NormalizeSignedAngle(
        float angle)
    {
        return
            Mathf.Repeat(
                angle + 180f,
                360f) - 180f;
    }

    private static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM

        if (Mouse.current == null)
        {
            return Vector2.zero;
        }

        /*
         * New Input Systemのdeltaはピクセル値なので、
         * Legacy Inputに近い操作量へ縮小する。
         */
        return
            Mouse.current.delta.ReadValue() *
            0.02f;

#elif ENABLE_LEGACY_INPUT_MANAGER

        return new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y"));

#else

        return Vector2.zero;

#endif
    }

    private static float ReadRollInput()
    {
#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
        {
            return 0f;
        }

        float input =
            0f;

        if (Keyboard.current.qKey.isPressed)
        {
            input -=
                1f;
        }

        if (Keyboard.current.eKey.isPressed)
        {
            input +=
                1f;
        }

        return input;

#elif ENABLE_LEGACY_INPUT_MANAGER

        float input =
            0f;

        if (Input.GetKey(KeyCode.Q))
        {
            input -=
                1f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            input +=
                1f;
        }

        return input;

#else

        return 0f;

#endif
    }

    private void OnValidate()
    {
        _mouseSensitivity.x =
            Mathf.Max(
                0f,
                _mouseSensitivity.x);

        _mouseSensitivity.y =
            Mathf.Max(
                0f,
                _mouseSensitivity.y);

        _rollSpeed =
            Mathf.Max(
                0f,
                _rollSpeed);
    }

    private void OnDestroy()
    {
        _stateMachine?.Shutdown();

        for (int i = 0;
             i < _volumes.Count;
             ++i)
        {
            CameraVolume volume =
                _volumes[i];

            if (volume != null)
            {
                volume.ReleaseRuntimeState();
            }
        }

        _volumes.Clear();

        if (_defaultRuntimeState != null)
        {
            Destroy(
                _defaultRuntimeState);

            _defaultRuntimeState =
                null;
        }

        _currentVolume =
            null;
    }

    /// <summary>
    /// XYZの角度をワールド軸回転として生成する。
    ///
    /// 適用順序:
    /// 1. ワールドX
    /// 2. ワールドY
    /// 3. ワールドZ
    /// </summary>
    public static Quaternion CreateWorldAxisRotation(
        Vector3 rotation)
    {
        Quaternion rotationX =
            Quaternion.AngleAxis(
                rotation.x,
                Vector3.right);

        Quaternion rotationY =
            Quaternion.AngleAxis(
                rotation.y,
                Vector3.up);

        Quaternion rotationZ =
            Quaternion.AngleAxis(
                rotation.z,
                Vector3.forward);

        /*
         * Quaternionは右側から適用されるため、
         * X → Y → Zの順番ではZ * Y * Xになる。
         */
        return
            rotationZ *
            rotationY *
            rotationX;
    }
}