using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(100)]
public sealed class CameraController : MonoBehaviour
{
	private const float MinimumAngle = -180f;
	private const float MaximumAngle = 180f;
	private const float FreeAxisTolerance = 0.001f;
	private const float DirectionEpsilon = 0.000001f;
	private const float InputEpsilon = 0.000001f;

	[Header("References")]

	[SerializeField]
	private Player _player;

	[SerializeField]
	private Camera _controlledCamera;

	[Header("Default State")]

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
     * X = Pitch
     * Y = Yaw
     * Z = Roll
     */
	private Vector3 _currentOrbitRotation;
	private Vector3 _targetOrbitRotation;

	/*
     * State進入直後の同一フレームで、
     * 復元したOrbit角度が即座にClipされるのを防ぐ。
     */
	private bool _deferOrbitClipOnce;

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

	/// <summary>
	/// 今フレームにOrbit入力が存在したか。
	/// </summary>
	public bool HasOrbitInputThisFrame
	{
		get;
		private set;
	}

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

		UpdateCurrentVolume();
	}

	private void FixedUpdate()
	{
		_stateMachine?.FixedUpdate();
	}

	private void LateUpdate()
	{
		UpdateCurrentVolume();

		_stateMachine?.Update();
	}

	/// <summary>
	/// 現在位置に対応するCameraVolumeを選択し、
	/// 必要な場合にStateを切り替える。
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

		bool keepCurrentBlendState =
			currentState is CameraBlendState &&
			nextState is CameraBlendState;

		_currentVolume =
			selectedVolume;

		if (keepCurrentBlendState)
		{
			/*
             * BlendState自体は維持するが、
             * 主要なBlend Volumeが変化したため、
             * 進入Smoothnessを再度少し弱める。
             */
			CameraBlendState blendState =
				currentState as CameraBlendState;

			blendState?.RestartEnterTransition();

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
	/// 入力をOrbit角度へ即時反映し、
	/// Updateの最後でRotationLimitへClipする。
	/// </summary>
	public Vector3 UpdateOrbitAngles(
		Vector3 rotationLimitMin,
		Vector3 rotationLimitMax,
		float deltaTime)
	{
		HasOrbitInputThisFrame =
			false;

		if (deltaTime <=
			0f)
		{
			return _currentOrbitRotation;
		}

		Vector2 mouseDelta =
			ReadMouseDelta();

		float rollInput =
			ReadRollInput();

		float pitchDirection =
			_invertVertical
				? 1f
				: -1f;

		HasOrbitInputThisFrame =
			mouseDelta.sqrMagnitude >
				InputEpsilon ||
			Mathf.Abs(
				rollInput) >
				InputEpsilon;

		Vector3 inputDelta =
			new Vector3(
				mouseDelta.y *
				_mouseSensitivity.y *
				pitchDirection,

				mouseDelta.x *
				_mouseSensitivity.x,

				rollInput *
				_rollSpeed *
				deltaTime);

		/*
         * 入力自体は即座に反映する。
         */
		_currentOrbitRotation +=
			inputDelta;

		if (_deferOrbitClipOnce)
		{
			/*
             * Enterと同じフレームではClipしない。
             */
			_deferOrbitClipOnce =
				false;
		}
		else
		{
			/*
             * 通常フレームでは厳密に角度制限を適用する。
             */
			_currentOrbitRotation =
				ClipOrbitAngles(
					_currentOrbitRotation,
					rotationLimitMin,
					rotationLimitMax);
		}

		_targetOrbitRotation =
			_currentOrbitRotation;

		return _currentOrbitRotation;
	}

	/// <summary>
	/// Orbit角度を直接設定する。
	/// </summary>
	public void SetOrbitRotation(
		Vector3 rotation)
	{
		_currentOrbitRotation =
			rotation;

		_targetOrbitRotation =
			rotation;

		_deferOrbitClipOnce =
			false;

		HasOrbitInputThisFrame =
			false;
	}

	/// <summary>
	/// State進入時に現在Camera位置から逆算した
	/// Orbit角度を設定する。
	/// </summary>
	public void InitializeOrbitAngles(
		Vector3 currentAngles,
		Vector3 rotationLimitMin,
		Vector3 rotationLimitMax)
	{
		/*
         * Enter時点ではClipせず、
         * 現在の見た目を維持する。
         */
		_currentOrbitRotation =
			currentAngles;

		_targetOrbitRotation =
			currentAngles;

		_deferOrbitClipOnce =
			true;

		HasOrbitInputThisFrame =
			false;
	}

	/// <summary>
	/// Orbit角度をRotationLimit内へClipする。
	/// </summary>
	public static Vector3 ClipOrbitAngles(
		Vector3 angles,
		Vector3 rotationLimitMin,
		Vector3 rotationLimitMax)
	{
		angles.x =
			ClipOrbitAxis(
				angles.x,
				rotationLimitMin.x,
				rotationLimitMax.x);

		angles.y =
			ClipOrbitAxis(
				angles.y,
				rotationLimitMin.y,
				rotationLimitMax.y);

		angles.z =
			ClipOrbitAxis(
				angles.z,
				rotationLimitMin.z,
				rotationLimitMax.z);

		return angles;
	}

	private static float ClipOrbitAxis(
		float angle,
		float minimum,
		float maximum)
	{
		if (IsFreeAxis(
				minimum,
				maximum))
		{
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

	/// <summary>
	/// TPS用のOrbit回転を生成する。
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
	/// 現在Camera位置からPitchとYawを逆算する。
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

		Vector3 pitchAxis =
			yawedBaseRotation *
			Vector3.right;

		if (pitchAxis.sqrMagnitude <=
			DirectionEpsilon)
		{
			return false;
		}

		pitchAxis.Normalize();

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

		Vector2 lookDelta = Vector2.zero;

		// この関数が呼ばれている間はカーソルを中央に固定する
		if (Cursor.lockState != CursorLockMode.Locked)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		// マウス入力
		if (Mouse.current != null)
		{
			lookDelta +=
				Mouse.current.delta.ReadValue() *
				0.02f;
		}

		// コントローラー右スティック入力
		if (Gamepad.current != null)
		{
			const float stickDeadZone = 0.15f;
			const float stickSensitivity = 2.0f;

			Vector2 stickInput =
				Gamepad.current.rightStick.ReadValue();

			// スティックドリフト防止
			if (stickInput.sqrMagnitude >=
				stickDeadZone * stickDeadZone)
			{
				// スティックは移動量ではなく入力強度なので、
				// DeltaTimeを掛けてフレームレート非依存にする
				lookDelta +=
					stickInput *
					stickSensitivity *
					Time.unscaledDeltaTime *
					30.0f;
			}
		}

		return lookDelta;

#elif ENABLE_LEGACY_INPUT_MANAGER

	if (Cursor.lockState != CursorLockMode.Locked)
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

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
