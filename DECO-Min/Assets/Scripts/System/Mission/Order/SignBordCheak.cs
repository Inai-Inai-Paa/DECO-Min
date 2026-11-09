using UnityEngine;
using UnityEngine.InputSystem;

public class SignBordCheak : MonoBehaviour
{
    private bool _enterFlag = false;

    private void Update()
    {
        if (_enterFlag)
        {
            if (Mouse.current != null && 
                Mouse.current.leftButton.wasPressedThisFrame)
            {

                // ミッションが開始されていないときのみ
                if (!Missionmanager.Instance.missionStartFlg)
                {
                    Debug.Log("Collision OK");
                    MissionOrderSystem.Instance.OrderPanelOpen();
                }
            }
        }
    }

    /// <summary> 看板チェックできる判定内 </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _enterFlag = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _enterFlag = false;
        }
    }
}
    


