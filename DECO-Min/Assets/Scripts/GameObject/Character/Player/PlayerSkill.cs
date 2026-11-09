///
///メモ
///今の所外部から指定しないけど、今後出てきそうだからscriptableにしたよ
///

using UnityEngine;

[CreateAssetMenu(menuName = "Skill")]
public class PlayerSkill : ScriptableObject
{
    [SerializeField] public bool _canUseWall = true; //ふさぐ
    [SerializeField] public bool _canUseJump = true; //ジャンプ
    [SerializeField] public bool _canUseBalloon = true; //剥がす

    public bool CanUseWall =>_canUseWall;
    public bool CanUseJump =>_canUseJump;
    public bool CanUseBalloon => _canUseBalloon;
}

