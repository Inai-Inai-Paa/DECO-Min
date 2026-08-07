using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[CreateAssetMenu(menuName = "State/Player/TrampolineMove")]
public class PlayerTrampoline : PlayerState
{
    [Header("Transition State")]
    [SerializeField] private PlayerState _moveState;

    [Header("Approach Settings")]
    [SerializeField] private float _approachSpeed = 2.0f;        // 歩く速度 (m/s)
    [SerializeField] private float _rotationSpeed = 10.0f;       // 向きを合わせる速さ
    [SerializeField] private float _approachThreshold = 0.5f;    // 到達とみなす距離

    private SplineAnimate _animate;
    private bool _isPlaying = false;

    private bool _isApproaching = false;
    private bool _isFacing = false;
    private Vector3 _targetPosition;
    private Vector3 _faceDirection; // 向くべき方向

    public override void Enter()
    {
        base.Enter();

        _animate = player.GetComponent<SplineAnimate>();

        // スプライン起点位置を取得。
        if (_animate != null && _animate.Container != null)
        {
            _targetPosition = _animate.Container.EvaluatePosition(0.0f);
            _faceDirection =  _animate.Container.EvaluatePosition(1.0f) - _animate.Container.EvaluatePosition(0.0f);
        }
        else
        {
            // フォールバック：プレイヤー自身の位置（即時再生）
            _targetPosition = player.transform.position;
            _faceDirection = player.transform.forward;
        }

        _isApproaching = true;
        _isFacing = false;
        _isPlaying = false;

    }

    public override void Update()
    {
        base.Update();

        // 回転チェックは Update で滑らかに行う
        if (!_isApproaching && !_isPlaying && _isFacing)
        {

            TryStartSplinePlay();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (_isApproaching)
        {
            Vector3 dir = _targetPosition - player.transform.position;
            float dist = dir.magnitude;

            Debug.Log($"Approaching: Distance to target = {dist}, Threshold = {_approachThreshold}");
            if (dist > _approachThreshold)
            {
                Vector3 moveDir = dir.normalized;
                player.additionalVelocity += moveDir * _approachSpeed;
                Vector3 targetForward = new Vector3(moveDir.x, 0f, moveDir.z);

                if (targetForward.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(targetForward.normalized, Vector3.up);
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, _rotationSpeed * Time.fixedDeltaTime);
                }
            }
            else
            {
                // 到達したらアプローチ終了、向きをスプライン進行方向へ合わせるステップへ
                _isApproaching = false;
                _isFacing = false;
            }
        }
        else if (!_isPlaying)
        {
            //進行方向に向かせる
            Vector3 targetForward = _faceDirection;
            targetForward.y = 0f;
            if (targetForward.sqrMagnitude < 0.001f)
                targetForward = player.transform.forward;

            Quaternion targetRot = Quaternion.LookRotation(targetForward.normalized, Vector3.up);
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, _rotationSpeed * Time.fixedDeltaTime);

            // 向きが十分揃ったら再生開始
            float angle = Quaternion.Angle(player.transform.rotation, targetRot);
            Debug.Log($"Angle to target: {angle}");
            if (angle < 5.0f) // しきい値（度）
            {
                _isFacing = true;
                TryStartSplinePlay();
            }
        }

        // 再生中の終了判定
        if (_isPlaying && _animate != null && _animate.IsPlaying == false)
        {
            _isPlaying = false;
            player.ChangePlayerState(Instantiate(_moveState));
        }
    }

    private void TryStartSplinePlay()
    {
        if (_isPlaying) return;
        if (_animate == null) return;

        _animate.Restart(false);
        _animate.Play();
        _isPlaying = true;
    }

    public override void Exit()
    {
        base.Exit();
        _animate.Container = null;
    }
}