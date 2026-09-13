using UnityEngine;

public class CrabHand : MonoBehaviour
{

    [SerializeField] GameObject hand;
    public Material ChargeMaterial;
    public Material NormalMaterial;
	void Start()
    {
        
    }

    void Update()
    {
        
    }
    public void SetHandMaterial(bool isCharging)
    {
        if (isCharging)
        {
            hand.GetComponent<Renderer>().material = ChargeMaterial;
        }
        else
        {
            hand.GetComponent<Renderer>().material = NormalMaterial;
        }
	}

}
