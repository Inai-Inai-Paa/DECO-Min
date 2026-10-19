using UnityEngine;

[CreateAssetMenu(menuName = "Mission/SubMission")]
public class SubMission : ScriptableObject
{
    [Header("ƒ~ƒbƒVƒ‡ƒ“İ’è")]
    [SerializeField] public SubMissionPreset missionPreset;
    [SerializeField] public string missionText;

    [Tooltip("ŠÔ§ŒÀ(•b)")]
    [SerializeField] public float timeLimit = 60;

    [SerializeField] public GameObject reward;
}
