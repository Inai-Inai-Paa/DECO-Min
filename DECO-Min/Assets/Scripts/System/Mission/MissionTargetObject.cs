using UnityEngine;

[CreateAssetMenu(menuName = "Mission/MissionTargetObject")]
public class MissionTargetObject : ScriptableObject
{
    [Header("ƒ~ƒbƒVƒ‡ƒ“İ’è")]
    [SerializeField] public MissionPreset missionPreset;
    [SerializeField] public string MissionText;
}
