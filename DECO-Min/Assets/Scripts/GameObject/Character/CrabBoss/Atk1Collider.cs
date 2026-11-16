using UnityEngine;

public class Atk1Collider : MonoBehaviour
{
    void Start()
    {
        this.gameObject.SetActive(true);
        Destroy(this.gameObject, 0.5f);
	}

    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player != null)
            {
                player.TryDamage(10);
            }

            Debug.Log("攻撃1の当たり判定がプレイヤーに命中しました。");
        }
	}
}
