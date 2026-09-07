using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    public static MissionUIManager Instance { get; private set; }

    [SerializeField] private GameObject _missionUIRoot;

    [SerializeField] private MissionUILine _uILine;

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
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
    }

    public void DrawMissionUI()
    {
       _uILine.Initialize(Missionmanager.Instance.targetObject.Object.missionText);
       _uILine.UpdateText();
    }

    public void SetMissionRoot(bool flag)
    {
        _missionUIRoot.SetActive(flag);
        DrawMissionUI();
    }

}
