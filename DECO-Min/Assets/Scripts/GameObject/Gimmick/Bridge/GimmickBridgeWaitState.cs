using UnityEngine;

[CreateAssetMenu(menuName = "State/Gimmick/Bridge/Wait")]
public class GimmickBridgeWaitState : GimmickState
{
    private GimmickBridge _bridgeGimmick;

    public override void Enter()
    {
        _bridgeGimmick = gimmick as GimmickBridge;

        if (_bridgeGimmick != null)
        {
            _bridgeGimmick._interactUI.SetActive(false);
        }
    }

    public override void Update()
    {
        if(_bridgeGimmick._playerInside)
        {
            _bridgeGimmick.ChangeGimmickState(Instantiate(_bridgeGimmick._readyState));
        }
    }
}
