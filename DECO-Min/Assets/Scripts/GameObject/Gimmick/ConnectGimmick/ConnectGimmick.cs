using UnityEngine;

public class ConnectGimmick : Gimmick
{
	public GameObject InteractHitBox;

	public GameObject InteractUIText;

	public GameObject InteractUISlider;

	public GameObject GimmikArea;
	public bool playerInside { get; private set; } = false;

	public GimmickState InteractState;
	public GimmickState ActivateState;

	protected override void Start()
	{
		base.Start();
		ChangeState(InteractState);
	}

	private void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("Player"))
		{
			player = other.GetComponent<Player>();
			playerInside = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if(other.CompareTag("Player"))
		{
			Player _player = other.GetComponent<Player>();
			if(player == _player)
			{
				player = null;
				playerInside = false;
			}
		}
	}
}
