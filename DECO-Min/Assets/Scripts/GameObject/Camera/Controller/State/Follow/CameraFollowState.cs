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

	[SerializeField]
	private bool _useUnscaledTime;

	/*
     * 0 = 進入直後
     * 1 = 通常Smoothnessへ完全復帰
     */
	private float _enterTransitionProgress;

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
		_enterTransitionProgress =
			0f;

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

		orbitAngles.z =
			_allowRoll
				? CalculateCurrentCameraRoll(
					_camera.transform,
					pivotPosition)
				: 0f;

		_controller.InitializeOrbitAngles(
			orbitAngles,
			config.rotationLimitMin,
			config.rotationLimitMax);
	}

	public override void Exit()
	{
	}

	public override void FixedUpdate()
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

		if (deltaTime <=
			0f)
		{
			return;
		}

		CameraVolume.CameraParams config =
			_volume.Config;

		GetEffectiveSmoothness(
			config,
			deltaTime,
			out float smoothnessPosition,
			out float transitionSmoothnessTarget);

		Vector3 orbitAngles =
			_controller.UpdateOrbitAngles(
				config.rotationLimitMin,
				config.rotationLimitMax,
				deltaTime);

		if (!_allowRoll)
		{
			orbitAngles.z =
				0f;
		}

		float effectiveSmoothnessTarget =
			_controller.HasOrbitInputThisFrame
				? Mathf.Max(
					0.01f,
					config.smoothnessTarget)
				: transitionSmoothnessTarget;

		float positionInterpolationRate =
			CalculateInterpolationRate(
				smoothnessPosition,
				deltaTime);

		float targetInterpolationRate =
			CalculateInterpolationRate(
				effectiveSmoothnessTarget,
				deltaTime);

		Quaternion orbitRotation =
			CameraController.CreateCameraOrbitRotation(
				_volume.transform.rotation,
				orbitAngles);

		Vector3 pivotPosition =
			GetPivotPosition();

		Vector3 targetCameraPosition =
			pivotPosition +
			orbitRotation *
			config.offset;

		/*
         * 地面より下に入る目標位置を先に補正する。
         */
		targetCameraPosition =
			ApplyGroundPush(
				targetCameraPosition);

		Transform cameraTransform =
			_camera.transform;

		cameraTransform.position =
			Vector3.Lerp(
				cameraTransform.position,
				targetCameraPosition,
				positionInterpolationRate);

		/*
         * 補間途中でも地面にめり込まないように、
         * 実際のCamera位置にも押し出しを適用する。
         */
		cameraTransform.position =
			ApplyGroundPush(
				cameraTransform.position);

		Quaternion targetCameraRotation =
			CreateStableLookRotation(
				cameraTransform.position,
				pivotPosition,
				cameraTransform.rotation);

		if (_allowRoll)
		{
			targetCameraRotation *=
				Quaternion.AngleAxis(
					orbitAngles.z,
					Vector3.forward);
		}

		cameraTransform.rotation =
			Quaternion.Slerp(
				cameraTransform.rotation,
				targetCameraRotation,
				targetInterpolationRate);

		if (!_camera.orthographic)
		{
			float targetFieldOfView =
				Mathf.Clamp(
					config.fieldOfView,
					1f,
					179f);

			_camera.fieldOfView =
				Mathf.Lerp(
					_camera.fieldOfView,
					targetFieldOfView,
					positionInterpolationRate);
		}
	}

	private void GetEffectiveSmoothness(
		CameraVolume.CameraParams config,
		float deltaTime,
		out float smoothnessPosition,
		out float smoothnessTarget)
	{
		float recoveryDuration =
			Mathf.Max(
				0f,
				config.enterSmoothnessRecoveryDuration);

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
					config.enterSmoothnessPosition),
				Mathf.Max(
					0.01f,
					config.smoothnessPosition),
				transitionRate);

		smoothnessTarget =
			Mathf.Lerp(
				Mathf.Max(
					0.01f,
					config.enterSmoothnessTarget),
				Mathf.Max(
					0.01f,
					config.smoothnessTarget),
				transitionRate);
	}

	private Vector3 GetPivotPosition()
	{
		return
			_target.position +
			_pivotOffset;
	}

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

		cameraPosition =
			ApplyGroundPushForPreview(
				cameraPosition);

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