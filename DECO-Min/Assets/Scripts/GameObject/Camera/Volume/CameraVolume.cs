
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CameraVolume : MonoBehaviour
{
	[System.Serializable]
	public struct CameraParams
	{
		[Tooltip("カメラが地形にめり込まないか")]
		public bool preventTerrainPenetration;

		[Tooltip("カメラがめり込まない地形のレイヤーマスク")]
		public LayerMask terrainLayerMask;

		[Tooltip("Originのオフセット")]
		public Vector3 targetOffset;

		[Tooltip("Originから見たカメラのローカル位置")]
		public Vector3 cameraOffset;

		[Range(1f, 179f)]
		public float fieldOfView;

		[Header("Normal Smoothness")]

		[Tooltip(
			"通常時のカメラ位置追従速度。\n" +
			"値が大きいほど素早く追従します。")]
		[Min(0.01f)]
		public float smoothnessPosition;

		[Tooltip(
			"通常時のカメラ回転・注視方向の追従速度。\n" +
			"値が大きいほど素早く追従します。")]
		[Min(0.01f)]
		public float smoothnessTarget;

		[Header("Enter Transition")]

		[Tooltip(
			"このVolumeへ進入した直後に使用する、" +
			"カメラ位置のSmoothness")]
		[Min(0.01f)]
		public float enterSmoothnessPosition;

		[Tooltip(
			"このVolumeへ進入した直後に使用する、" +
			"カメラ回転・注視方向のSmoothness")]
		[Min(0.01f)]
		public float enterSmoothnessTarget;

		[Tooltip(
			"進入用Smoothnessから通常Smoothnessへ" +
			"復帰するまでの時間")]
		[Min(0f)]
		public float enterSmoothnessRecoveryDuration;

		[Header("Rotation Limit")]

		[Tooltip(
			"各軸の最小操作角度。\n" +
			"値は-180〜180度で指定します。")]
		public Vector3 rotationLimitMin;

		[Tooltip(
			"各軸の最大操作角度。\n" +
			"Min=-180、Max=180の軸は完全自由になります。")]
		public Vector3 rotationLimitMax;
	}

	[Header("Volume Settings")]

	[Min(0.01f)]
	[SerializeField]
	private float _radius =
		20f;

	[Tooltip("Volumeが重なった場合の選択優先度")]
	[SerializeField]
	private int _priority;

	[Header("Camera State")]

	[SerializeField]
	private CameraState _stateTemplate;

	[Header("Camera Settings")]

	[SerializeField]
	private CameraParams _config =
		new CameraParams
		{
			preventTerrainPenetration =
				true,

			terrainLayerMask = 
				1,

			targetOffset =
				new Vector3(
					0f,
					1.5f,
					0f),

			cameraOffset =
				new Vector3(
					0f,
					5f,
					-10f),

			fieldOfView =
				60f,

			smoothnessPosition =
				5f,

			smoothnessTarget =
				20f,

			enterSmoothnessPosition =
				1.5f,

			enterSmoothnessTarget =
				2f,

			enterSmoothnessRecoveryDuration =
				3f,

			rotationLimitMin =
				new Vector3(
					-5f,
					-15f,
					0f),

			rotationLimitMax =
				new Vector3(
					45f,
					15f,
					0f)
		};

	private CameraController _controller;
	private CameraState _runtimeState;

	public float Radius =>
		_radius;

	public int Priority =>
		_priority;

	public CameraParams Config =>
		_config;

	public CameraState StateTemplate =>
		_stateTemplate;

	public CameraState RuntimeState =>
		_runtimeState;

	public void Initialize(
		CameraController controller)
	{
		ReleaseRuntimeState();

		_controller =
			controller;

		if (_controller == null)
		{
			Debug.LogError(
				$"{name}: CameraControllerがnullです。",
				this);

			return;
		}

		if (_stateTemplate == null)
		{
			Debug.LogWarning(
				$"{name}: CameraStateが設定されていません。",
				this);

			return;
		}

		/*
         * ScriptableObjectのStateアセットを直接使用すると、
         * 複数Volume間で実行時参照が共有される。
         *
         * Volumeごとに専用インスタンスを生成する。
         */
		_runtimeState =
			Instantiate(
				_stateTemplate);

		_runtimeState.name =
			$"{_stateTemplate.name} Runtime ({name})";

		_runtimeState.hideFlags =
			HideFlags.HideAndDontSave;

		_runtimeState.Initialize(
			_controller,
			_controller.ControlledCamera,
			_controller.StateMachine,
			_controller.Target,
			this);
	}

	public bool Contains(
		Vector3 worldPosition)
	{
		if (_radius <=
			Mathf.Epsilon)
		{
			return false;
		}

		Vector3 difference =
			worldPosition -
			transform.position;

		return
			difference.sqrMagnitude <=
			_radius *
			_radius;
	}

	public float GetSquaredDistance(
		Vector3 worldPosition)
	{
		Vector3 difference =
			worldPosition -
			transform.position;

		return difference.sqrMagnitude;
	}

	public bool GetGroundPosition(
		out Vector3 groundPosition)
	{
		groundPosition =
			transform.position;

		const float maxDistance =
			500f;

		if (!Physics.Raycast(
				transform.position,
				Vector3.down,
				out RaycastHit hit,
				maxDistance,
				~0,
				QueryTriggerInteraction.Ignore))
		{
			return false;
		}

		groundPosition =
			hit.point;

		return true;
	}

	public bool UsesState<T>()
		where T : CameraState
	{
		return
			_runtimeState is T;
	}

	public void ReleaseRuntimeState()
	{
		if (_runtimeState != null)
		{
			if (Application.isPlaying)
			{
				Destroy(
					_runtimeState);
			}
			else
			{
				DestroyImmediate(
					_runtimeState);
			}

			_runtimeState =
				null;
		}

		_controller =
			null;
	}

	private void OnDestroy()
	{
		ReleaseRuntimeState();
	}

	private void OnValidate()
	{
		_radius =
			Mathf.Max(
				0.01f,
				_radius);

		CameraParams config =
			_config;

		config.fieldOfView =
			Mathf.Clamp(
				config.fieldOfView,
				1f,
				179f);

		config.smoothnessPosition =
			Mathf.Max(
				0.01f,
				config.smoothnessPosition);

		config.smoothnessTarget =
			Mathf.Max(
				0.01f,
				config.smoothnessTarget);

		config.enterSmoothnessPosition =
			Mathf.Max(
				0.01f,
				config.enterSmoothnessPosition);

		config.enterSmoothnessTarget =
			Mathf.Max(
				0.01f,
				config.enterSmoothnessTarget);

		config.enterSmoothnessRecoveryDuration =
			Mathf.Max(
				0f,
				config.enterSmoothnessRecoveryDuration);

		config.rotationLimitMin =
			ClampRotationLimit(
				config.rotationLimitMin);

		config.rotationLimitMax =
			ClampRotationLimit(
				config.rotationLimitMax);

		SortMinMax(
			ref config.rotationLimitMin.x,
			ref config.rotationLimitMax.x);

		SortMinMax(
			ref config.rotationLimitMin.y,
			ref config.rotationLimitMax.y);

		SortMinMax(
			ref config.rotationLimitMin.z,
			ref config.rotationLimitMax.z);

		_config =
			config;
	}

	private static Vector3 ClampRotationLimit(
		Vector3 value)
	{
		value.x =
			Mathf.Clamp(
				value.x,
				-180f,
				180f);

		value.y =
			Mathf.Clamp(
				value.y,
				-180f,
				180f);

		value.z =
			Mathf.Clamp(
				value.z,
				-180f,
				180f);

		return value;
	}

	private static void SortMinMax(
		ref float minimum,
		ref float maximum)
	{
		if (minimum <=
			maximum)
		{
			return;
		}

		float temporary =
			minimum;

		minimum =
			maximum;

		maximum =
			temporary;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color =
			Color.cyan;

		Gizmos.DrawWireSphere(
			transform.position,
			Mathf.Max(
				0f,
				_radius));
	}
}

