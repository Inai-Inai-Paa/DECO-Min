using UnityEngine;

public class UIEnemyBase : UIBase
{
	[Header("Player")]
	[SerializeField]
	protected CharacterStatus _enemyStatus;

	protected override void Start()
	{
		base.Start();

        _enemyStatus = gameObject.GetComponentInChildren<CharacterStatus>();
	}

	// Update is called once per frame
	void Update()
	{
		
	}


}
