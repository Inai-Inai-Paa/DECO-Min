using UnityEngine;

public class BossActiveArea : MonoBehaviour
{
	private Player PlayerObj;
	[SerializeField] Character Boss;
	void Start()
	{
		PlayerObj = FindObjectOfType<Player>();
	}

	void Update()
	{

	}

	void OnTriggerStay(Collider other)
	{
		Boss.GetComponent<CrabBoss>().IsActive = other.gameObject == PlayerObj.gameObject;
	}

}
