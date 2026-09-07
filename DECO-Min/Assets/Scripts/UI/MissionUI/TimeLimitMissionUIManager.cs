using UnityEngine;

public class TimeLimitMissionUIManager : MonoBehaviour
{
    public static TimeLimitMissionUIManager Instance { get; private set; }

    [SerializeField] private GameObject _missionUIs;

    [SerializeField] private TimeLimitMissionUILine _uILine;

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
