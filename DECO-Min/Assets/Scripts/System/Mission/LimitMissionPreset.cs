using UnityEngine;

[System.Serializable]
public class LimitMissionPreset
{
    public enum MissionTpye
    {
        Defeat,
        Collect,
        Place,
    }

    public MissionTpye missionType;

    public int objectCount;
    public GameObject targetObject;
}
