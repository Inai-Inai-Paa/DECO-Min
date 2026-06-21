
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

    private const float DirectionEpsilon =
        0.000001f;

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

    [Tooltip(
        "XはYaw感度、YはPitch感度")]
    [SerializeField]
    private Vector2 _mouseSensitivity =
        new Vector2(
            4f,
            4f);

    [SerializeField]
    private bool _invertVertical;

    [Tooltip(
        "Q/EによるRoll回転速度")]
    [SerializeField]
    private float _rollSpeed =
        60f;

    private readonly List<CameraVolume> _volumes =
        new List<CameraVolume>();

    private StateMachine _stateMachine;

    private CameraState _defaultRuntimeState;
    private CameraVolume _currentVolume;

    /*
     * CameraStateを跨いで維持される操作角度。
     *
     * X = Pitch
     * Y = Yaw
     * Z = Roll
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

        SetOrbitRotation(
            Vector3.zero);

        CreateDefaultState();
        InitializeVolumes();

        if (_defaultRuntimeState != null)
        {
            _stateMachine.ChangeState(
                _defaultRuntimeState);
        }

        /*
         * ゲーム開始時からVolume内にいる場合にも対応する。
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
         * プレイヤー移動後にVolumeを判定する。
         */
        UpdateCurrentVolume();

        _stateMachine?.Update();
    }

    /// <summary>
    /// 現在位置に対応するCameraVolumeを選択し、
    /// 必要な場合だけStateを切り替える。
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

        if (_currentVolume ==
            selectedVolume)
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
         * BlendStateは全Blend Volumeをまとめて扱う。
         *
         * Blend Volume同士で選択対象が変わっただけなら、
         * RuntimeStateを切り替えない。
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

        if (nextState != null)
        {
            _stateMachine.ChangeState(
                nextState);
        }
    }

    /// <summary>
    /// 指定位置を含むVolumeから、
    /// 優先度と中心距離を使って1つ選択する。
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
    /// カメラ入力を反映し、
    /// 制限と補間を適用したOrbit角度を返す。
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

        /*
         * マウスY入力はPitchへ加算する。
         */
        _targetOrbitRotation.x +=
            mouseDelta.y *
            _mouseSensitivity.y *
            pitchDirection;

        /*
         * マウスX入力はYawへ加算する。
         */
        _targetOrbitRotation.y +=
            mouseDelta.x *
            _mouseSensitivity.x;

        /*
         * Q/E入力はRollへ加算する。
         */
        _targetOrbitRotation.z +=
            ReadRollInput() *
            _rollSpeed *
            deltaTime;

        /*
         * 目標角度だけを制限する。
         *
         * 現在角度が制限外にあっても即座にClampせず、
         * Smoothnessによって制限内へ戻す。
         */
        _targetOrbitRotation =
            ConstrainRotation(
                _targetOrbitRotation,
                rotationLimitMin,
                rotationLimitMax);

        float interpolationRate =
            CalculateInterpolationRate(
                smoothness,
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
    /// 現在角度と目標角度を同じ値へ設定する。
    /// </summary>
    public void SetOrbitRotation(
        Vector3 rotation)
    {
        _currentOrbitRotation =
            rotation;

        _targetOrbitRotation =
            rotation;
    }

    /// <summary>
    /// State進入時に逆算したOrbit角度を設定する。
    ///
    /// 現在角度は逆算値をそのまま維持し、
    /// 目標角度だけをRotationLimit内へ制限する。
    /// </summary>
    public void InitializeOrbitAngles(
        Vector3 currentAngles,
        Vector3 rotationLimitMin,
        Vector3 rotationLimitMax)
    {
        _currentOrbitRotation =
            currentAngles;

        _targetOrbitRotation =
            ConstrainRotation(
                currentAngles,
                rotationLimitMin,
                rotationLimitMax);
    }

    /// <summary>
    /// TPS用のOrbit回転を生成する。
    ///
    /// Yaw:
    ///     ワールドY軸
    ///
    /// Pitch:
    ///     Yaw適用後のカメラ右軸
    ///
    /// Roll:
    ///     Pitch適用後のカメラ前方軸
    /// </summary>
    public static Quaternion CreateCameraOrbitRotation(
        Quaternion baseRotation,
        Vector3 orbitAngles)
    {
        Quaternion yawRotation =
            Quaternion.AngleAxis(
                orbitAngles.y,
                Vector3.up);

        Quaternion yawedRotation =
            yawRotation *
            baseRotation;

        Vector3 pitchAxis =
            yawedRotation *
            Vector3.right;

        if (pitchAxis.sqrMagnitude <=
            DirectionEpsilon)
        {
            pitchAxis =
                Vector3.right;
        }

        pitchAxis.Normalize();

        Quaternion pitchRotation =
            Quaternion.AngleAxis(
                orbitAngles.x,
                pitchAxis);

        Quaternion pitchedRotation =
            pitchRotation *
            yawedRotation;

        Vector3 rollAxis =
            pitchedRotation *
            Vector3.forward;

        if (rollAxis.sqrMagnitude <=
            DirectionEpsilon)
        {
            rollAxis =
                Vector3.forward;
        }

        rollAxis.Normalize();

        Quaternion rollRotation =
            Quaternion.AngleAxis(
                orbitAngles.z,
                rollAxis);

        return
            rollRotation *
            pitchedRotation;
    }

    /// <summary>
    /// 現在のカメラ位置から、
    /// 指定されたOrigin・BaseRotation・Offsetに対応する
    /// PitchとYawを逆算する。
    ///
    /// 距離は一致していなくてもよく、
    /// カメラの方向を基準に角度を求める。
    /// </summary>
    public static bool TryCalculateOrbitAnglesFromPosition(
        Vector3 currentCameraPosition,
        Vector3 pivotPosition,
        Quaternion baseRotation,
        Vector3 cameraOffset,
        out Vector3 orbitAngles)
    {
        orbitAngles =
            Vector3.zero;

        Vector3 currentWorldOffset =
            currentCameraPosition -
            pivotPosition;

        Vector3 baseWorldOffset =
            baseRotation *
            cameraOffset;

        if (currentWorldOffset.sqrMagnitude <=
                DirectionEpsilon ||
            baseWorldOffset.sqrMagnitude <=
                DirectionEpsilon)
        {
            return false;
        }

        /*
         * 基準Offsetと現在OffsetをXZ平面へ投影し、
         * ワールドY軸周りのYawを求める。
         */
        Vector3 baseHorizontal =
            Vector3.ProjectOnPlane(
                baseWorldOffset,
                Vector3.up);

        Vector3 currentHorizontal =
            Vector3.ProjectOnPlane(
                currentWorldOffset,
                Vector3.up);

        float yaw =
            0f;

        if (baseHorizontal.sqrMagnitude >
                DirectionEpsilon &&
            currentHorizontal.sqrMagnitude >
                DirectionEpsilon)
        {
            yaw =
                Vector3.SignedAngle(
                    baseHorizontal,
                    currentHorizontal,
                    Vector3.up);
        }

        Quaternion yawRotation =
            Quaternion.AngleAxis(
                yaw,
                Vector3.up);

        Quaternion yawedBaseRotation =
            yawRotation *
            baseRotation;

        Vector3 yawedBaseOffset =
            yawRotation *
            baseWorldOffset;

        /*
         * PitchはYaw後の基準姿勢の右軸を使う。
         */
        Vector3 pitchAxis =
            yawedBaseRotation *
            Vector3.right;

        if (pitchAxis.sqrMagnitude <=
            DirectionEpsilon)
        {
            return false;
        }

        pitchAxis.Normalize();

        /*
         * Pitch軸方向の成分を除去し、
         * Pitch平面上で符号付き角度を求める。
         */
        Vector3 basePitchPlane =
            Vector3.ProjectOnPlane(
                yawedBaseOffset,
                pitchAxis);

        Vector3 currentPitchPlane =
            Vector3.ProjectOnPlane(
                currentWorldOffset,
                pitchAxis);

        float pitch =
            0f;

        if (basePitchPlane.sqrMagnitude >
                DirectionEpsilon &&
            currentPitchPlane.sqrMagnitude >
                DirectionEpsilon)
        {
            pitch =
                Vector3.SignedAngle(
                    basePitchPlane,
                    currentPitchPlane,
                    pitchAxis);
        }

        orbitAngles =
            new Vector3(
                NormalizeSignedAngle(
                    pitch),

                NormalizeSignedAngle(
                    yaw),

                0f);

        return true;
    }

    /// <summary>
    /// 実行中に追加・削除されたVolumeを再取得する。
    /// </summary>
    public void RefreshVolumes()
    {
        _currentVolume =
            null;

        if (_defaultRuntimeState != null)
        {
            _stateMachine?.ChangeState(
                _defaultRuntimeState);
        }

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
             * 完全自由軸では連続角度を維持する。
             */
            return angle;
        }

        return Mathf.Clamp(
            NormalizeSignedAngle(
                angle),
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

    private static float CalculateInterpolationRate(
        float smoothness,
        float deltaTime)
    {
        if (smoothness <= Mathf.Epsilon)
        {
            return 1f;
        }

        return
            1f -
            Mathf.Exp(
                -smoothness *
                deltaTime);
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
         * Legacy Inputに近い量へ縮小する。
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
}

