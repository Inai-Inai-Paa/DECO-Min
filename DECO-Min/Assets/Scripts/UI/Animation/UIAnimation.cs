using System.Collections;
using UnityEngine;

public class UIAnimation : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private bool _useScale = true;
    [SerializeField] private Vector3 _scaleAmount = new Vector3(1.2f, 1.2f, 1.0f);

    [Header("Position")]
    [SerializeField] private bool _usePosition = false;
    [SerializeField] private Vector3 _moveAmount = Vector3.zero;

    [Header("Rotation")]
    [SerializeField] private bool _useRotation = false;
    [SerializeField] private Vector3 _rotationAmount = Vector3.zero;

    [Header("Animation")]
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private bool _loop = false;

    [SerializeField]
    private AnimationCurve _curve = new AnimationCurve(
        new Keyframe(0.0f, 0.0f),
        new Keyframe(0.5f, 1.0f),
        new Keyframe(1.0f, 0.0f)
    );

    private Vector3 _defaultScale;
    private Vector3 _defaultPosition;
    private Quaternion _defaultRotation;

    private Coroutine _animationCoroutine;

    private void Awake()
    {
        _defaultScale = transform.localScale;
        _defaultPosition = transform.localPosition;
        _defaultRotation = transform.localRotation;
    }

    public void Play()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        ResetTransform();

        _animationCoroutine = StartCoroutine(PlayAnimation());
    }

    public void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        ResetTransform();
    }

    private IEnumerator PlayAnimation()
    {
        do
        {
            float time = 0.0f;

            while (time < _duration)
            {
                time += Time.deltaTime;

                float rate = Mathf.Clamp01(
                    time / _duration
                );

                float curveValue = _curve.Evaluate(rate);

                ApplyAnimation(curveValue);

                yield return null;
            }

            ResetTransform();
        }
        while (_loop);

        _animationCoroutine = null;
    }

    private void ApplyAnimation(float value)
    {
        if (_useScale)
        {
            Vector3 targetScale = Vector3.Scale(
                _defaultScale,
                _scaleAmount
            );

            transform.localScale = Vector3.LerpUnclamped(
                _defaultScale,
                targetScale,
                value
            );
        }

        if (_usePosition)
        {
            transform.localPosition = Vector3.LerpUnclamped(
                _defaultPosition,
                _defaultPosition + _moveAmount,
                value
            );
        }

        if (_useRotation)
        {
            Quaternion targetRotation = _defaultRotation * Quaternion.Euler(
                _rotationAmount
            );

            transform.localRotation = Quaternion.LerpUnclamped(
                _defaultRotation,
                targetRotation,
                value
            );
        }
    }

    private void ResetTransform()
    {
        transform.localScale = _defaultScale;
        transform.localPosition = _defaultPosition;
        transform.localRotation = _defaultRotation;
    }
}