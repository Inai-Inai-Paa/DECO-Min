using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mission/MIssionPreset")]
public class MIssionPreset : ScriptableObject
{
    [Header("メインミッション設定")]
    [SerializeField] public MainMissionPreset mainMission;

    [Header("中ミッション設定")]
    [SerializeField] public List<SubMissionPreset> subMissions;
  
}
