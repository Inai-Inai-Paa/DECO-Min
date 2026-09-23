using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Advances waves, tracks defeat progress, and drives spawners.
/// </summary>
public class EnemyWaveController
{
    private IReadOnlyList<EnemyWave> _waves;
    private EnemySpawner[] _allSpawners;
    private EnemyRegistry _registry;
    private float _nextWaveDelay;
    private int _fallbackMaxAliveCount = 5;

    private int _currentWaveIndex = -1;
    private int _defeatedCountInWave;
    private int _totalDefeatedCount;
    private bool _isWaveRunning;
    private bool _hasSpawnedInWave;
    private float _nextWaveTimer;

    public event Action<int> WaveStarted;
    public event Action<int> WaveEnded;
    public event Action AllEnemiesDefeated;
    public event Action<Enemy> EnemyDiedInWave;

    public int CurrentWaveIndex => _currentWaveIndex;
    public bool IsWaveRunning => _isWaveRunning;
    public int TotalDefeatedCount => _totalDefeatedCount;
    public int MaxAliveCount => GetCurrentMaxAliveCount();
    public int RemainingEnemyCount => GetRemainingEnemyCount();
    public bool CanSpawnEnemy => _registry != null && _registry.Count < MaxAliveCount;

    public void Initialize(
        IReadOnlyList<EnemyWave> waves,
        EnemySpawner[] allSpawners,
        EnemyRegistry registry,
        float nextWaveDelay,
        int fallbackMaxAliveCount)
    {
        _waves = waves;
        _allSpawners = allSpawners;
        _registry = registry;
        _nextWaveDelay = nextWaveDelay;
        _fallbackMaxAliveCount = Mathf.Max(1, fallbackMaxAliveCount);

        if (_registry != null)
        {
            _registry.EnemyDied -= HandleEnemyDied;
            _registry.EnemyDied += HandleEnemyDied;
        }
    }

    public void Dispose()
    {
        if (_registry != null)
        {
            _registry.EnemyDied -= HandleEnemyDied;
        }
    }

    public void Tick(float deltaTime)
    {
        if (_isWaveRunning)
        {
            UpdateWaveClearCheck();
            return;
        }

        if (_nextWaveTimer <= 0.0f)
        {
            return;
        }

        _nextWaveTimer -= deltaTime;

        if (_nextWaveTimer <= 0.0f)
        {
            StartWave(_currentWaveIndex + 1);
        }
    }

    public void NotifyEnemySpawned()
    {
        _hasSpawnedInWave = true;
    }

    public void StartWave(int waveIndex)
    {
        if (waveIndex < 0)
        {
            return;
        }

        int waveCount = _waves != null ? _waves.Count : 0;

        if (waveCount > 0 && waveIndex >= waveCount)
        {
            AllEnemiesDefeated?.Invoke();
            return;
        }

        _currentWaveIndex = waveIndex;
        _defeatedCountInWave = 0;
        _hasSpawnedInWave = false;
        _isWaveRunning = true;
        _nextWaveTimer = 0.0f;

        StopAllSpawners();
        StartCurrentWaveSpawners();
        WaveStarted?.Invoke(_currentWaveIndex);
    }

    public void EndCurrentWave()
    {
        if (!_isWaveRunning)
        {
            return;
        }

        _isWaveRunning = false;
        StopAllSpawners();
        WaveEnded?.Invoke(_currentWaveIndex);

        int waveCount = _waves != null ? _waves.Count : 0;

        if (waveCount == 0 || _currentWaveIndex + 1 >= waveCount)
        {
            AllEnemiesDefeated?.Invoke();
        }
        else
        {
            _nextWaveTimer = _nextWaveDelay;
        }
    }

    public void Stop()
    {
        _isWaveRunning = false;
        _nextWaveTimer = 0.0f;
        StopAllSpawners();
    }

    public bool ContainsSpawner(EnemySpawner spawner)
    {
        if (spawner == null)
        {
            return false;
        }

        EnemyWave wave = GetCurrentWave();

        if (wave != null && !wave.UseAllSpawners)
        {
            return ContainsSpawner(wave.Spawners, spawner);
        }

        return ContainsSpawner(_allSpawners, spawner);
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        _defeatedCountInWave++;
        _totalDefeatedCount++;
        EnemyDiedInWave?.Invoke(enemy);
        UpdateWaveClearCheck();
    }

    private void UpdateWaveClearCheck()
    {
        if (!_isWaveRunning)
        {
            return;
        }

        EnemyWave wave = GetCurrentWave();

        if (wave != null && wave.TargetDefeatCount > 0)
        {
            if (_defeatedCountInWave >= wave.TargetDefeatCount)
            {
                EndCurrentWave();
            }

            return;
        }

        if (_hasSpawnedInWave && _registry != null && _registry.Count <= 0)
        {
            EndCurrentWave();
        }
    }

    private void StopAllSpawners()
    {
        if (_allSpawners == null)
        {
            return;
        }

        foreach (EnemySpawner spawner in _allSpawners)
        {
            if (spawner != null)
            {
                spawner.StopWave();
            }
        }
    }

    private void StartCurrentWaveSpawners()
    {
        EnemyWave wave = GetCurrentWave();

        if (wave == null || wave.UseAllSpawners)
        {
            StartSpawners(_allSpawners);
            return;
        }

        StartSpawners(wave.Spawners);
    }

    private void StartSpawners(IReadOnlyList<EnemySpawner> spawners)
    {
        if (spawners == null)
        {
            return;
        }

        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.StartWave();
            }
        }
    }

    private int GetCurrentMaxAliveCount()
    {
        EnemyWave wave = GetCurrentWave();
        return wave != null ? wave.MaxAliveCount : _fallbackMaxAliveCount;
    }

    private int GetRemainingEnemyCount()
    {
        EnemyWave wave = GetCurrentWave();

        if (wave == null || wave.TargetDefeatCount <= 0)
        {
            return _registry != null ? _registry.Count : 0;
        }

        return Mathf.Max(0, wave.TargetDefeatCount - _defeatedCountInWave);
    }

    private EnemyWave GetCurrentWave()
    {
        if (_waves == null || _currentWaveIndex < 0 || _currentWaveIndex >= _waves.Count)
        {
            return null;
        }

        return _waves[_currentWaveIndex];
    }

    private static bool ContainsSpawner(IReadOnlyList<EnemySpawner> spawners, EnemySpawner target)
    {
        if (spawners == null)
        {
            return false;
        }

        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner == target)
            {
                return true;
            }
        }

        return false;
    }
}
