using UnityEngine;

[CreateAssetMenu(menuName = "State/Gimmick/DestroyGimmick/Passive")]
public class DestroyGimmickPassiveState : GimmickState
{
    private DestroyGimmick _DestroyGimmick;

    public override void Enter()
    {
        _DestroyGimmick = gimmick as DestroyGimmick;

        if (_DestroyGimmick != null)
        {
            _DestroyGimmick._interactUI.SetActive(false);
        }
    }

    public override void Update()
    {
        if (_DestroyGimmick._playerInside)
        {
            _DestroyGimmick.ChangeGimmickState(Instantiate(_DestroyGimmick._activeState));
        }
    }
}
