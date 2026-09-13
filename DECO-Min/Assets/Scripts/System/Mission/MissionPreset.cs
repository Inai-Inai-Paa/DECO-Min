using UnityEngine;

[System.Serializable]
public class MissionPreset
{
    public enum MissionTpye
    {
        Boss,
        Defeat,
        Collect,
    }

    public MissionTpye missionType;

    public int objectCount;
    public GameObject targetObject;
}
