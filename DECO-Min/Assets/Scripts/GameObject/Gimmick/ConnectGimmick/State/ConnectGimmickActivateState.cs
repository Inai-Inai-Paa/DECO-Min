using UnityEngine;

[CreateAssetMenu(fileName = "Activate", menuName = "State/Gimmick/Connect/Activate")]
public class ConnectGimmickActivateState : GimmickState
{
	public override void Enter()
	{
		ConnectGimmick connectGimmick = gimmick as ConnectGimmick;
		connectGimmick.InteractHitBox.SetActive(false);
		connectGimmick.InteractUIText.SetActive(false);
		connectGimmick.InteractUISlider.SetActive(false);
		connectGimmick.GimmikArea.SetActive(true);
	}

	public override void Update()
	{
		ConnectGimmick connectGimmick = gimmick as ConnectGimmick;
		if (!connectGimmick.playerInside)
		{
			connectGimmick.ChangeState(connectGimmick.InteractState);
		}
	}
}
