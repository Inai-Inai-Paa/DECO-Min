using UnityEngine;

[System.Serializable]
public class MissionPreset
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

    public EnemyData killObject;
    public CollectData collectObject;
    public int timeLimit;
    
    public string missionText;

    public GameObject reward;
    public int rewardSeal;
}
