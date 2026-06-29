using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    public static MissionUIManager Instance { get; private set; }

    [SerializeField] private GameObject _missionUIRoot;

    [SerializeField] private string _missionText;

    [SerializeField] private MissionUILine _uILine;

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Start()
    {
        DrawMissionUI();
    }

    public void DrawMissionUI()
    {
       _uILine.Initialize(Missionmanager.Instance.targetObject.MissionText);
       _uILine.UpdateText();
    }

    public void SetMissionRoot(bool flag)
    {
        _missionUIRoot.SetActive(flag);
    }

    public string GetMissionText()
    {
        return _missionText;
    }
}
