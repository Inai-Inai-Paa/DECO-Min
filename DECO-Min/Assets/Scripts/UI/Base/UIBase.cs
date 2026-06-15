using UnityEngine;

public class UIBase : MonoBehaviour
{
    [Header("UIBase")]
    [SerializeField]
	protected Canvas _canvas;

	[SerializeField]
    protected Animator _animator;

	protected virtual void Start()
	{
		_canvas = GetComponent<Canvas>();
		
		_animator = gameObject.GetComponent<Animator>();

		gameObject.SetActive(false);
	}

	void Update()
	{
		
	}

	/// <summary>
	/// ï`âÊÇ∑ÇÈÇ©ìIÇ»ä÷êîÇΩÇø
	/// </summary>
	protected virtual void ShowCanvas()
	{
		_canvas.enabled = true;
	}

	protected virtual void HideCanvas() 
	{
		_canvas.enabled = false;
	}
    protected virtual void Show()
    {
        gameObject.SetActive(true);
    }

    protected virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
