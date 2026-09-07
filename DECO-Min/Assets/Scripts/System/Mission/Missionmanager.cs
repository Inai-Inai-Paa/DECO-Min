using UnityEngine;

public class Missionmanager : MonoBehaviour
{
    public static Missionmanager Instance { get; private set; }

    public bool missionStartFlg = false;

    private bool MissionClear = false; 
    [SerializeField] public MainMissionTargetObject targetObject;

    private KillCounter _KillCounter;
    private CollectCounter _CollectCounter;

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
        Instance = this;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {
        // ‰Šú‰»
        MissionClear = false;

        _KillCounter = new KillCounter();
        _CollectCounter = new CollectCounter();
    }

    public void SetMission(MainMissionTargetObject missionTargetObject)
    {
        targetObject = missionTargetObject;
    }
    public void Clear()
    {
       MissionClear = true;
    }

    public void Killed()
    {
        Clear();
    }

    public void AddKill(EnemyData data)
    {
        _KillCounter.AddKillCount(data);
        for (int i = 0; i < targetObject.subMissions.Count; i++)
        {
            CheakSubMissionClear(i);
        }
    }

    public void AddCollect(CollectData data)
    {
        _CollectCounter.AddCollectCount(data);
        for (int i = 0; i < targetObject.subMissions.Count; i++)
        {
            CheakSubMissionClear(i);
        }
    }

    private void CheakSubMissionClear(int i)
    {
        var submission = targetObject.subMissions[i];
        if (!submission.isClear)
        {
            switch (submission.missionType)
            {
                case LimitMissionPreset.MissionTpye.Defeat:
                    if (submission.objectCount < _KillCounter.GetKillCount(submission.KillObject))
                    {
                        submission.isClear = true;
                    }
                    break;
                case LimitMissionPreset.MissionTpye.Collect:
                    if (submission.objectCount < _CollectCounter.GetCollectCount(submission.CollectObject))
                    {
                        submission.isClear = true;
                    }
                    break;
            }
        }
    }

    public void SetMissionStartFlg(bool flg)
    {
        missionStartFlg = flg;
    }

}
