using System.Collections.Generic;
using UnityEngine;

public class BossActiveArea : MonoBehaviour
{
	[SerializeField] Character Boss;
	private CrabBoss bossV1;
	private CrabBoss2_0 bossV2;
	private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
	private Collider areaCollider;
	private Player player;

	void Awake()
	{
		areaCollider = GetComponent<Collider>();
	}

	void Update()
	{
		if (areaCollider == null) return;

		if (player == null)
		{
			player = FindFirstObjectByType<Player>();
		}
		if (player == null) return;

		SetBossActive(IsPlayerOverlappingArea());
	}

	void OnTriggerEnter(Collider other)
	{
		RegisterPlayerCollider(other);
	}

	void OnTriggerStay(Collider other)
	{
		RegisterPlayerCollider(other);
	}

	void OnTriggerExit(Collider other)
	{
		if (other.GetComponentInParent<Player>() == null) return;

		playerColliders.Remove(other);
		SetBossActive(playerColliders.Count > 0);
	}

	void RegisterPlayerCollider(Collider other)
	{
		if (other.GetComponentInParent<Player>() == null) return;

		playerColliders.Add(other);
		SetBossActive(true);
	}

	void SetBossActive(bool isActive)
	{
		if (bossV2 == null && Boss != null)
		{
			bossV2 = Boss.GetComponent<CrabBoss2_0>();
		}
		if (bossV2 == null)
		{
			bossV2 = FindFirstObjectByType<CrabBoss2_0>();
		}
		if (bossV2 != null)
		{
			bossV2.SetActive(isActive);
			return;
		}

		if (bossV1 == null && Boss != null)
		{
			bossV1 = Boss.GetComponent<CrabBoss>();
		}
		if (bossV1 != null)
		{
			bossV1.IsActive = isActive;
		}
	}

	bool IsPlayerOverlappingArea()
	{
		foreach (Collider playerCollider in player.GetComponentsInChildren<Collider>())
		{
			if (playerCollider.enabled && playerCollider.gameObject.activeInHierarchy &&
				areaCollider.bounds.Intersects(playerCollider.bounds))
			{
				return true;
			}
		}

		return false;
	}
}
