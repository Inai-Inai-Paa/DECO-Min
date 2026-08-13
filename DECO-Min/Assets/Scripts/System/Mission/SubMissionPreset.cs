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

    public bool isClear = false;

    public int objectCount;
    public GameObject targetObject;

    public EnemyData KillObject;
    public CollectData CollectObject;
    public int timeLimit;
}
