using UnityEngine;

public class Order: MonoBehaviour
{
    [Header("ˆË—Š‚·‚éƒ~ƒbƒVƒ‡ƒ“İ’è")]
    [SerializeField] private MissionTargetObject _missionRequest;

    void RequestMission()
    {
        Missionmanager.Instance.targetObject = _missionRequest;
    }
}
