using UnityEngine;
using UnityEngine.Splines;

public class Trampoline : Gimmick
{
    [Header("Trampoline Ref")]
    [Tooltip("トランポリン挙動のスプライン"), SerializeField] private SplineContainer _trampolineCurve;

    [Header("Trampoline Setting")]
    [Tooltip("トランポリンの基点"),SerializeField] private GameObject _basePoint;
    [Tooltip("トランポリンの終着地点"), SerializeField] private GameObject _landingPoint;

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
}
