using UnityEngine;

[CreateAssetMenu(menuName = "Mission/MainMissionTargetObject")]
public class MainMissionTargetObject : ScriptableObject
{
    [Header("メインミッション設定")]
    [SerializeField] public MainMissionPreset Object;

    [SerializeField] public string missionText;
}
