using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable wave settings used by EnemyWaveController.
/// </summary>
[Serializable]
public class EnemyWave
{
    [SerializeField] private string _waveName = "Wave";
    [SerializeField, Min(0)] private int _targetDefeatCount = 0;
    [SerializeField, Min(1)] private int _maxAliveCount = 5;
    [SerializeField] private bool _useAllSpawners = true;
    [SerializeField] private EnemySpawner[] _spawners;

    public string WaveName => _waveName;
    public int TargetDefeatCount => _targetDefeatCount;
    public int MaxAliveCount => _maxAliveCount;
    public bool UseAllSpawners => _useAllSpawners;
    public IReadOnlyList<EnemySpawner> Spawners => _spawners;
}
