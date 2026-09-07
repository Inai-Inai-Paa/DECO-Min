using UnityEngine;

public class SignBordCheak : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {


        if (!Missionmanager.Instance.missionStartFlg)
        {
              Debug.Log("Collision OK");
            MissionOrderSystem.Instance.OrderPanelOpen();
        }
    }
}
    


