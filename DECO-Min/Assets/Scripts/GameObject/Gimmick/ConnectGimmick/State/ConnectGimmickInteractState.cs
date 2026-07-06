using UnityEngine;

[CreateAssetMenu(fileName = "Interact", menuName = "State/Gimmick/Connect/Interact")]
public class ConnectGimmickInteractState : GimmickState
{
	[SerializeField]
	private float interactTimeMax = 2f;

	private float interactTime = 0f;
	private UnityEngine.UI.Slider _slider;

	public override void Enter()
	{
		interactTime = 0f;

		ConnectGimmick connectGimmick = gimmick as ConnectGimmick;
		connectGimmick.InteractHitBox.SetActive(true);
		connectGimmick.InteractUIText.SetActive(true);
		connectGimmick.InteractUISlider.SetActive(true);
		connectGimmick.GimmikArea.SetActive(false);
		_slider = connectGimmick.InteractUISlider.GetComponent<UnityEngine.UI.Slider>();
		SetSlider(interactTime / interactTimeMax);
	}

	public override void Update()
	{
		SetSlider(interactTime / interactTimeMax);

		ConnectGimmick connectGimmick = gimmick as ConnectGimmick;
		if (connectGimmick.playerInside)
		{
			if(connectGimmick.player.playerInputData.InteractPressed)
			{
				interactTime += Time.deltaTime;
				if(interactTime >= interactTimeMax)
				{
					// Perform interaction logic here
					interactTime = 0f;
					connectGimmick.ChangeState(connectGimmick.ActivateState);
				}
			}
			else
			{
				interactTime -= Time.deltaTime;
			}
		}
		else
		{
			interactTime -= Time.deltaTime;
		}
		if(interactTime < 0f)
		{
			interactTime = 0f;
		}
	}

	private void SetSlider(float value)
	{
		if(_slider == null)
		{
			return;
		}
		_slider.value = Mathf.Clamp01(value);
	}
}
