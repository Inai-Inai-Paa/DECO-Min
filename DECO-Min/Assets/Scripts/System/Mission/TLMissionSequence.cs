using System;
using UnityEngine;

public class TLMissionInstance
{
    public TimeLimitMission mission;
    public bool _isActive = false;
    public float _time;
    public int _targetCount;

    public event Action<TLMissionInstance> OnStart;
    public event Action<TLMissionInstance> OnUpdate;
    public event Action<TLMissionInstance> OnSuccess;
    public event Action<TLMissionInstance> OnFaild;

    public TLMissionInstance(TimeLimitMission mission)
    {
        this.mission = mission;
        _time = mission.timeLimit;
    }
    public void Start()
    {
        _isActive = true;
        OnStart?.Invoke(this);
    }


    public void CountDown(float dt)
    {
        if (!_isActive)
        {
            return;
        }
        _time -= dt;
        OnUpdate?.Invoke(this);
        if (_time <= 0)
        {
            Fail();
        }
    }

    public void AddCount(int value)
    {
        _targetCount += value;
    }

    public void Success()
    {
        if (!_isActive) return;
        _isActive = false;
        OnSuccess?.Invoke(this);
    }

    public void Fail()
    {
        if (!_isActive) return;
        _isActive = false;
        OnFaild?.Invoke(this);
    }
}
