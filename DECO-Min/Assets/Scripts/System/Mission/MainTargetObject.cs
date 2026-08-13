using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mission/MainMissionTargetObject")]
public class MainMissionTargetObject : ScriptableObject
{
    [Header("メインミッション設定")]
    [SerializeField] public MainMissionPreset Object;

    [Header("中ミッション設定")]
    [SerializeField] public List<LimitMissionPreset> subMissions;
    [SerializeField] public string missionText;
}
