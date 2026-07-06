using UnityEngine;

public abstract class CameraState : State
{
	[Header("Ground Push")]

	[Tooltip("カメラが地面にめり込むのを防ぐかどうか")]
	[SerializeField]
	private bool _pushGround =
		true;

	[Tooltip("地面から最低限離す距離")]
	[Min(0f)]
	[SerializeField]
	private float _groundClearance =
		0.15f;

	[Tooltip("カメラ位置の上方向から地面を探す高さ")]
	[Min(0.01f)]
	[SerializeField]
	private float _groundProbeHeight =
		3f;

	[Tooltip("地面として扱うLayer")]
	[SerializeField]
	private LayerMask _groundMask =
		~0;

	[Tooltip("Trigger Colliderを地面判定に含めるか")]
	[SerializeField]
	private QueryTriggerInteraction _groundTriggerInteraction =
		QueryTriggerInteraction.Ignore;

	protected CameraController _controller;
	protected Camera _camera;
	protected StateMachine _stateMachine;
	protected Transform _target;
	protected CameraVolume _volume;

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

	public override void Enter()
	{
	}

	public override void Exit()
	{
	}

	public override void Update()
	{
	}

	public override void FixedUpdate()
	{
	}

	public virtual bool TryGetPreview(
		CameraVolume volume,
		out CameraPreviewData preview)
	{
		preview =
			default;

		return false;
	}

	/// <summary>
	/// Cameraの目標位置が地面より下にある場合、
	/// 最低クリアランス分だけ上へ押し出す。
	///
	/// Follow/Blendなどの継承先Stateは、
	/// targetCameraPositionを計算した直後と、
	/// 補間後の実Camera位置にこの関数を通す。
	/// </summary>
	protected Vector3 ApplyGroundPush(
		Vector3 cameraPosition)
	{
		if (!_pushGround)
		{
			return cameraPosition;
		}

		Vector3 rayOrigin =
			cameraPosition +
			Vector3.up *
			_groundProbeHeight;

		float rayDistance =
			_groundProbeHeight +
			Mathf.Max(
				0f,
				_groundClearance) +
			100f;

		if (!Physics.Raycast(
				rayOrigin,
				Vector3.down,
				out RaycastHit hit,
				rayDistance,
				_groundMask,
				_groundTriggerInteraction))
		{
			return cameraPosition;
		}

		float minimumY =
			hit.point.y +
			Mathf.Max(
				0f,
				_groundClearance);

		if (cameraPosition.y >=
			minimumY)
		{
			return cameraPosition;
		}

		cameraPosition.y =
			minimumY;

		return cameraPosition;
	}

	/// <summary>
	/// Editor Preview用。
	/// Runtimeと同じ地面押し出しを使う。
	/// </summary>
	protected Vector3 ApplyGroundPushForPreview(
		Vector3 cameraPosition)
	{
		return ApplyGroundPush(
			cameraPosition);
	}

	protected virtual void OnValidate()
	{
		_groundClearance =
			Mathf.Max(
				0f,
				_groundClearance);

		_groundProbeHeight =
			Mathf.Max(
				0.01f,
				_groundProbeHeight);
	}
}
