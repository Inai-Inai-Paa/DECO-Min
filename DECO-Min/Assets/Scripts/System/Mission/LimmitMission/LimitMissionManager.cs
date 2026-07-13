using System.Collections.Generic;
using UnityEngine;

public class LimitMissionManager : MonoBehaviour
{
    public static LimitMissionManager Instance;

    [SerializeField] private List<TimeLimitMission> _tlMissionSO;
    private readonly List<TLMissionInstance> _tlMissions = new();


    private KillCounter _killCounter;
 
     
    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
        Instance = this;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _killCounter = new KillCounter();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        for (int i = _tlMissions.Count - 1; i >= 0; i--)
        {
            var m = _tlMissions[i];
            m.CountDown(dt);

            if (!m._isActive)
            {
                _tlMissions.RemoveAt(i);
            }
        }
    }

    public TLMissionInstance StartTimeLimitMission(TimeLimitMission mission)
    {
        var instance = new TLMissionInstance(mission);
        _tlMissions.Add(instance);
        instance.Start();
        return instance;
    }




    public KillCounter GetKillCounter()
    {
        return _killCounter;
    }
}
