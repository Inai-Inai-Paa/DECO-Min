using UnityEngine;

public class UIPlayerBase : UIBase
{
	[Header("Player")]
	[SerializeField]
	protected CharacterStatus _playerStatus;

	protected override void Start()
	{
		base.Start();

        _playerStatus = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterStatus>();
	}

	// Update is called once per frame
	void Update()
	{
		
	}


}
