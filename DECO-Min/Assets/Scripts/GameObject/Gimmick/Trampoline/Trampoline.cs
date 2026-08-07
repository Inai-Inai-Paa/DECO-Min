using UnityEngine;
using UnityEngine.Splines;

public class Trampoline : Gimmick
{
    [Header("Trampoline Ref")]
    [Tooltip("トランポリン挙動のスプライン"), SerializeField] private SplineContainer _trampolineCurve;

    [Header("Trampoline Setting")]
    [Tooltip("トランポリンの基点"),SerializeField] private GameObject _basePoint;
    [Tooltip("トランポリンの終着地点"), SerializeField] private GameObject _landingPoint;
    [Tooltip("スプラインの移動スピード"),SerializeField] private float _splineMoveSpeed = 15.0f;

    [Header("UI Setting")]
    [Tooltip("インタラクトUI"),SerializeField] private Canvas _interactUI;
    [Tooltip("インタラクトUIの位置(足場基点)"), SerializeField] private Vector3 _interactUIOffset;

    private bool isPlayerOnTrampoline = false;
    private bool onStartPoint = false;

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

        if(_interactUI == null)
        {
            Debug.LogError("InteractUIが設定されていません。");
        }
        _interactUI.gameObject.SetActive(false);

    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public void PlaySplineAnimate(bool reverse)
    {
        if(player == null) return;

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
            onStartPoint = isStartPoint;
        }
    }
}