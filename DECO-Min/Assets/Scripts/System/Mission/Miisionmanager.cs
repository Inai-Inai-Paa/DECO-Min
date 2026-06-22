using UnityEngine;

public class Miisionmanager : MonoBehaviour
{
    private MissionTargetObject _targetObject;

    public bool MissionClear = false; 
    void Start()
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
}
