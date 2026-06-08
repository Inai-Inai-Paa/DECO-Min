using UnityEngine;

[CreateAssetMenu(menuName = "Status/Player")]
public class PlayerStatus : CharacterStatus
{
    public int TotalSealCount = 0;              // 獲得シール数（累計）
    public int CurrentSealCount = 0;            // 所持シール数（現在所持）
}
