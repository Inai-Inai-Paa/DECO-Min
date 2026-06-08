using UnityEngine;

[System.Serializable]
public class PlayerStatus
{
    public int TotalSealCount = 0;              // 獲得シール数（累計）
    public int CurrentSealCount = 0;            // 所持シール数（現在所持）
    public float MoveSpeedMultiplier = 1.0f;    // 移動力係数
    public float AttackPowerMultiplier = 1.0f;  // 攻撃力係数
}
