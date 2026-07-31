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

        //プレイヤーが破壊できるかの確認をする
        if (_destroyGimmick.Player.Skill.CanUseBreak &&
            _destroyGimmick.Player.playerInputData.InteractPressed)
        {
            //破壊(戻す可能性を考えてSetActive)
            _destroyGimmick._target.SetActive(false);
            _destroyGimmick._interactUI.SetActive(false);
        }
    }
}
