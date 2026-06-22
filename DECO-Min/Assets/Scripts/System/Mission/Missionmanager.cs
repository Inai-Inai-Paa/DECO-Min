using UnityEngine;

public class Missionmanager : MonoBehaviour
{
    private bool MissionClear = false; 

    [SerializeField] private MissionTargetObject _targetObject;

     public static Missionmanager Instance { get; private set; }

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Clear()
    {
        MissionClear = true;
    }
    public void SetMissionTarget(MissionTargetObject targetObject)
    {
        _targetObject = targetObject;
    }
}
