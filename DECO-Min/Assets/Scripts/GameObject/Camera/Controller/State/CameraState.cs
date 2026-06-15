using UnityEngine;

public abstract class CameraState : State
{
	// 参照用のカメラとステートマシン
	protected Camera _camera;
	protected StateMachine _stateMachine;

	/// <summary>
	/// 初期化関数
	/// </summary>
	/// <param name="camera">参照用のカメラ</param>
	/// <param name="stateMachine">参照用のステートマシン</param>
	public void Initialize(Camera camera, StateMachine stateMachine)
	{
		this._camera = camera;
		this._stateMachine = stateMachine;
	}
}
