///
///メモ
///今の所外部から指定しないけど、今後出てきそうだからscriptableにしたよ
///

using UnityEngine;

[CreateAssetMenu(menuName = "Skill")]
public class PlayerSkill : ScriptableObject
{
    [SerializeField] private bool _canUseBlock = true; //ふさぐ
    [SerializeField] private bool _canUseJump = true; //ジャンプ
    [SerializeField] private bool _canUsePeal = true; //剥がす
    [SerializeField] private bool _canUsePaste = true; //貼る
    [SerializeField] private bool _canUseBreak = true; //壊す

    public bool CanUseBlock =>_canUseBlock;
    public bool CanUseJump =>_canUseJump;
    public bool CanUsePeal =>_canUsePeal;
    public bool CanUsePaste =>_canUsePaste;
    public bool CanUseBreak =>_canUseBreak; 
}

