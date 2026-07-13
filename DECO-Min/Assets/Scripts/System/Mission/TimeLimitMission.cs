using UnityEngine;

[CreateAssetMenu(menuName = "Mission/TimeLimitMission")]
public class TimeLimitMission : ScriptableObject
{
    [Header("ƒ~ƒbƒVƒ‡ƒ“İ’è")]
    [SerializeField] public LimitMissionPreset missionPreset;
    [SerializeField] public string MissionText;

    [Tooltip("ŠÔ§ŒÀ(•b)")]
    [SerializeField] public float timeLimit = 60;

}
