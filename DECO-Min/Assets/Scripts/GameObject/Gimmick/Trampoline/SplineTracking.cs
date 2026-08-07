using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class SplineTracking : MonoBehaviour
{
    [Header("Tracking")]
    [Tooltip("トラッキング対象の始点"), SerializeField] private Transform _startPos;
    [Tooltip("トラッキング対象の終点"), SerializeField] private Transform _endPos;
    [Tooltip("トラッキング時の位置補正"),SerializeField] private Vector3 _positionOffset = new Vector3(0.0f, 0.5f, 0.0f);

    [Header("Debug")]
    [Tooltip("実行時のアップデート設定"), SerializeField] private bool _updateInPlayMode = true;
    [Tooltip("位置閾値"),SerializeField, Min(0.000001f)] private float _positionThreshold = 0.001f;

    private SplineContainer _splineContainer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _splineContainer = GetComponent<SplineContainer>();

    }

    private void OnValidate()
    {
        _splineContainer = GetComponent<SplineContainer>();

    }

    // Update is called once per frame
    void Update()
    {
        if(!Application.isPlaying || _updateInPlayMode)
        {
            UpdateSpline();
        }
    }

    private void UpdateSpline()
    {
        if (_splineContainer == null) return;
        if(_startPos == null || _endPos == null) return;

        var spline = _splineContainer.Spline;
        if (spline.Count < 2) return;

        SetKnotPoint(spline, 0, _startPos.position + _positionOffset);
        SetKnotPoint(spline, spline.Count - 1, _endPos.position + _positionOffset);
    }

    private void SetKnotPoint(Spline spline, int knotIndex, Vector3 worldPosition)
    {
        Vector3 localPosition = _splineContainer.transform.InverseTransformPoint(worldPosition);

        BezierKnot knot = spline[knotIndex];

        Vector3 currentPosition = new Vector3(knot.Position.x, knot.Position.y, knot.Position.z);

        if((currentPosition - localPosition).sqrMagnitude <= _positionThreshold * _positionThreshold)
        {
            return;
        }

        knot.Position = new Vector3(localPosition.x, localPosition.y, localPosition.z);

        spline.SetKnot(knotIndex, knot);

    }
}
