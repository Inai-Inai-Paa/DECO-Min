using UnityEngine;

public class Missionmanager : MonoBehaviour
{
    public static Missionmanager Instance { get; private set; }

    private bool MissionClear = false; 
    [SerializeField] public MissionTargetObject targetObject;

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
        Instance = this;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Debug.Log("MissionManager¶¬");

    }

    private void Start()
    {
        // ‰Šú‰»
        MissionClear = false;
    }

    public void SetMission(MissionTargetObject missionTargetObject)
    {
        targetObject = missionTargetObject;
    }
    public void Clear()
    {
       MissionClear = true;
    }
   
}
