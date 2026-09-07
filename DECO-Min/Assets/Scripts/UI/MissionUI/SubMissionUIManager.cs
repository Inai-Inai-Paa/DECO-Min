using UnityEngine;

public class SubMissionUIManager : MonoBehaviour
{
    public static SubMissionUIManager Instance { get; private set; }

    [SerializeField] private GameObject _missionUIs;

    [SerializeField] private SubMissionUILine _uILine;

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }


    public void DrawMissionUI()
    {
       _uILine.Initialize(Missionmanager.Instance.targetObject.Object.missionText);
       _uILine.UpdateText();
    }



}
