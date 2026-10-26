using System.Collections.Generic;
using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    public static MissionUIManager Instance { get; private set; }

    [SerializeField] private GameObject _missionUIRoot;

    [SerializeField] private MissionUILine _uiLine;
    [SerializeField] private MissionUILine _uiText;

    int _currentMissionIndex = 0;

    private void Awake()
    {
        // シングルトン
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Start()
    {

        SetMissionRoot(false);

        // UIの中身の初期化更新

        var missionpreset = Missionmanager.Instance.GetMissionPreset(_currentMissionIndex);

        _uiLine.Initialize("( {0} / {1} )");
        _uiLine.UpdateText(0, missionpreset.objectCount);

        _uiText.Initialize(missionpreset.missionText);
        _uiText.UpdateText();
    }

    public void UpdateUILine()
    {
        // UIの中身の更新
        DrawMissionUI();

        var missionpreset = Missionmanager.Instance.GetMissionPreset(_currentMissionIndex);

        _uiLine.UpdateText(Missionmanager.Instance.GetCurrentCount(_currentMissionIndex), missionpreset.objectCount);

        _uiText.Initialize(missionpreset.missionText);
        _uiText.UpdateText();

        Debug.Log("text = " + missionpreset.missionText);
    }

    public void DrawMissionUI()
    {
        _currentMissionIndex = -1;
        for (int i = 0; i < Missionmanager.Instance.targetObject.subMissions.Count; i++)
        {
            // 現在のミッションかチェック
            if (!Missionmanager.Instance.targetObject.subMissions[i].isClear)
            {
                _currentMissionIndex = i;
                Debug.Log("now id >" + _currentMissionIndex);
                break;
            }

           

        }
        // 全ミッションクリア済
        if (_currentMissionIndex < 0)
        {
            SetMissionRoot(false);

            Debug.Log("befalse");
        }
    } 

   

    public void SetMissionRoot(bool flag)
    {
        _missionUIRoot.SetActive(flag);
    }

}
