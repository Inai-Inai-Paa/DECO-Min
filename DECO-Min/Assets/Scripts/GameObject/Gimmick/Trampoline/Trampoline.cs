using UnityEngine;
using UnityEngine.Splines;

public class Trampoline : Gimmick
{
    [Header("Player Staet Ref")]
    [Tooltip("プレイヤートランポリン状態"), SerializeField] private PlayerState _playerTrampolineState;

    [Header("Trampoline Ref")]
    [Tooltip("トランポリン挙動のスプライン(登り方向)"), SerializeField] private SplineContainer _LeaveCurve;
    [Tooltip("トランポリン挙動のスプライン(降り方向)"), SerializeField] private SplineContainer _ReturnCurve;

    [Header("Trampoline Setting")]
    [Tooltip("トランポリンの基点"),SerializeField] private GameObject _basePoint;
    [Tooltip("トランポリンの終着地点"), SerializeField] private GameObject _landingPoint;
    [Tooltip("スプラインの移動スピード"),SerializeField] private float _splineMoveSpeed = 15.0f;

    [Header("UI Setting")]
    [Tooltip("インタラクトUI"),SerializeField] private Canvas _interactUI;
    [Tooltip("インタラクトUIの位置(足場基点)"), SerializeField] private Vector3 _interactUIOffset;

    private bool _onStartPoint = false;
    private bool _isReverse = false;

    protected override void Start()
    {
        base.Start();

        if(_basePoint == null)
        {
            Debug.LogError("BasePointが設定されていません。");
        }
        if(_landingPoint == null)
        {
            Debug.LogError("LandingPointが設定されていません。");
        }

        if(_LeaveCurve == null)
        {
            Debug.LogError("LeaveCurveが設定されていません。");
        }

        if (_ReturnCurve == null)
        {
            Debug.LogError("ReturnCurveが設定されていません。");
        }

        if (_interactUI == null)
        {
            Debug.LogError("InteractUIが設定されていません。");
        }
        _interactUI.gameObject.SetActive(false);

        if(_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        }

    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if(_onStartPoint)
        {
            if (_player != null && _player.playerInputData.InteractPressed)
            {
                PlaySplineAnimate();
                _interactUI.gameObject.SetActive(false);
            }
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    private void PlaySplineAnimate()
    {
        if (_player == null) return;
        var spAnim = _player.GetComponent<SplineAnimate>();
        if(spAnim == null)
        {
            spAnim = _player.gameObject.AddComponent<SplineAnimate>();
        }
        
        if(!_isReverse)
        {
            spAnim.Container = _LeaveCurve;
        }
        else
        {
            spAnim.Container = _ReturnCurve;
        }

        spAnim.MaxSpeed = _splineMoveSpeed;
        spAnim.StartOffset = 0.0f;

        _player.ChangePlayerState(Instantiate(_playerTrampolineState));
    }

    public void ChangeUIActive(bool isActive, bool isStartPoint = false)
    {
        if(_interactUI != null)
        {
            _interactUI.gameObject.SetActive(isActive);
        }
        _interactUI.transform.position = (isStartPoint?_basePoint.transform.position:_landingPoint.transform.position) + _interactUIOffset;

        if(isActive)
        {
            _isReverse = !isStartPoint;
        }

        _onStartPoint = isActive;
    }
}