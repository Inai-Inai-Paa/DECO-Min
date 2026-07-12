using UnityEngine;

[CreateAssetMenu(menuName = "State/Gimmick/Seal Weight Drop")]
public class SealWeightDropActiveState : GimmickActiveState
{
    public override void FixedUpdate()
    {
        // ‘Î‰ž‚·‚éƒMƒ~ƒbƒNŒ^‚ðŽæ“¾
        SealWeightDropper sealWeightDropper = gimmick as SealWeightDropper;

        if (sealWeightDropper == null)
            return;

        sealWeightDropper.UpdateMovement();
    }
}