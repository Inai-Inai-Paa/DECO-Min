using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIAnimation))]
public class UIHealthHeart : MonoBehaviour
{
    [SerializeField] private Image _offImage;
    [SerializeField] private Image _onLeftImage;
    [SerializeField] private Image _onRightImage;

    private UIAnimation _animation;

    private void Awake()
    {
        _animation = GetComponent<UIAnimation>();
    }

    public void SetHealth(float value)
    {
        value = Mathf.Clamp01(value);

        _offImage.enabled = true;
        _onLeftImage.enabled = value >= 0.5f;
        _onRightImage.enabled = value >= 1.0f;
    }

    public void PlayAnimation()
    {
        _animation.Play();
    }

    public void StopAnimation()
    {
        _animation.Stop();
    }
}