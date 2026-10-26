using UnityEngine;
using UnityEngine.InputSystem;

public class Missionmanager : MonoBehaviour
{
    public static Missionmanager Instance { get; private set; }

    public bool missionStartFlg = false;

    private bool _missionClear = false; 
    [SerializeField] public MIssionPreset targetObject;

    private KillCounter _killCounter;
    private CollectCounter _collectCounter;

    private void Awake()
    {
        // シングルトン
        Instance = this;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {

        foreach (var mission in targetObject.subMissions)
        {
            mission.isClear = false;
        }
        // 初期化
        _missionClear = false;

        _killCounter = new KillCounter();
        _collectCounter = new CollectCounter();
    }


    public void SetMission(MIssionPreset missionTargetObject)
    {
        targetObject = missionTargetObject;
    }
    public void Clear()
    {
       _missionClear = true;
    }

    public void Killed()
    {
        Clear();
    }

    /// <summary> キルカウント＋ </summary>
    public void AddKill(EnemyData data)
    {
        _killCounter.AddKillCount(data);
        for (int i = 0; i < targetObject.subMissions.Count; i++)
        {
            CheakSubMissionClear(i);
        }
    }
    /// <summary> 収集カウント＋ </summary>
    public void AddCollect(CollectData data)
    {
        _collectCounter.AddCollectCount(data);
        for (int i = 0; i < targetObject.subMissions.Count; i++)
        {
            CheakSubMissionClear(i);
        }
    }

    /// <summary> 引数IDのミッションのクリアチェック </summary>
    private void CheakSubMissionClear(int i)
    {
        var submission = targetObject.subMissions[i];

        if (!submission.isClear)
        {
            switch (submission.missionType)
            {
                case SubMissionPreset.MissionTpye.Defeat:
                    if (submission.objectCount < _killCounter.GetKillCount(submission.KillObject))
                    {
                        submission.isClear = true;
                    }
                    break;
                case SubMissionPreset.MissionTpye.Collect:
                    if (submission.objectCount < _collectCounter.GetCollectCount(submission.CollectObject))
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

    public KillCounter GetKillCounter()
    {
        return _killCounter;
    }
    
    public CollectCounter GetCollectCounter()
    {
        return _collectCounter;
    }

    /// <summary> 設定されているミッション情報 </summary>
    public SubMissionPreset GetMissionPreset(int id)
    {
        return targetObject.subMissions[id];
    }

    /// <summary> 現在のカウント状況取得 </summary>
    public int GetCurrentCount(int id)
    {
        var submission = targetObject.subMissions[id];
        switch(submission.missionType)
        {
            case SubMissionPreset.MissionTpye.Defeat:
                Debug.Log(" counter ");
                return _killCounter.GetKillCount(submission.KillObject); 
            case SubMissionPreset.MissionTpye.Collect:
                return _collectCounter.GetCollectCount(submission.CollectObject); 
            default:
                return 0;
        }
    }
}
