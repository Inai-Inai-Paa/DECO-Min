using UnityEngine;

[CreateAssetMenu(menuName = "State/Gimmick/Bridge/Active")]
public class GimmickBridgeActiveState : GimmickState
{
    private GimmickBridge _bridgeGimmick;
    public float _coolTime = 0;
    [SerializeField] private float _maxTime = 2;

    public override void Enter()
    {
        _bridgeGimmick = gimmick as GimmickBridge;

        if (_bridgeGimmick != null)
        {
            _bridgeGimmick._interactUI.SetActive(true);
        }
    }

    public override void Update()
    {
        if (!_bridgeGimmick._playerInside)
        {
            _bridgeGimmick.ChangeGimmickState(Instantiate(_bridgeGimmick._waitState));
            return;
        }

        if (_bridgeGimmick.Player.playerInputData.InteractPressed && _coolTime == 0)
        {
            //‹´‚ð•\Ž¦
            _bridgeGimmick._bridge.SetActive(!_bridgeGimmick._bridge.activeSelf);

            _coolTime = _maxTime;
            _bridgeGimmick._interactUI.SetActive(false);
        }

        if (_coolTime != 0)
        {
            _coolTime -= Time.deltaTime;
            if( _coolTime < 0 ) 
            {
                Enter();
                _coolTime = 0; 
            }
        }
    }
}
