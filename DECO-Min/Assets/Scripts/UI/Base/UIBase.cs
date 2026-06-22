using UnityEngine;

/// <summary>
/// UIの表示・非表示を共通化する基底クラス。
/// </summary>
public class UIBase : MonoBehaviour
{
    [Header("UI Base")]

    [SerializeField]
    protected Canvas _canvas;


    protected virtual void Start()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        if (_canvas == null)
        {
            Debug.LogWarning($"{nameof(UIBase)} : Canvas が見つかりません。UIがCanvas配下にあるか確認してください。");
        }

        Hide();
    }

    public virtual void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (_canvas != null)
        {
            _canvas.enabled = true;
        }
    }

    public virtual void Hide()
    {
        if (_canvas != null)
        {
            _canvas.enabled = false;
        }

        gameObject.SetActive(false);
    }
}