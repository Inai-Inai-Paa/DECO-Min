using UnityEngine;

[CreateAssetMenu(menuName = "State/Gimmick/DestroyGimmick/Active")]
public class DestroyGimmickActiveState : GimmickState
{
    private DestroyGimmick _destroyGimmick;

    public override void Enter()
    {
        _destroyGimmick = gimmick as DestroyGimmick;

        if (_destroyGimmick != null)
        {
            _destroyGimmick._interactUI.SetActive(true);
        }
    }

    public override void Update()
    {
        if (!_destroyGimmick._playerInside)
        {
            _destroyGimmick.ChangeGimmickState(Instantiate(_destroyGimmick._passiveState));
            return;
        }
    }
}
