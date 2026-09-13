using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 敵全体の登録、出現数、Wave進行を管理するクラス
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Serializable]
    public class EnemyEvent : UnityEvent<Enemy>
    {
    }

    [Serializable]
    public class WaveEvent : UnityEvent<int>
    {
    }

    [Serializable]
    private class EnemyWave
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

    public static EnemyManager Instance { get; private set; }

    [Header("参照")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private EnemySpawner[] _spawners;
    [SerializeField] private bool _autoFindPlayer = true;
    [SerializeField] private bool _autoFindSpawners = true;

    [Header("敵数管理")]
    [SerializeField, Min(1)] private int _maxAliveCount = 5;

    [Header("Wave")]
    [SerializeField] private bool _startWaveOnStart = true;
    [SerializeField, Min(0.0f)] private float _nextWaveDelay = 1.0f;
    [SerializeField] private List<EnemyWave> _waves = new List<EnemyWave>();

    [Header("通知")]
    [SerializeField] private EnemyEvent _onEnemyDied = new EnemyEvent();
    [SerializeField] private WaveEvent _onWaveStarted = new WaveEvent();
    [SerializeField] private WaveEvent _onWaveEnded = new WaveEvent();
    [SerializeField] private UnityEvent _onAllEnemiesDefeated = new UnityEvent();

    private readonly List<Enemy> _aliveEnemies = new List<Enemy>();
    private int _currentWaveIndex = -1;
    private int _defeatedCountInWave;
    private int _totalDefeatedCount;
    private bool _isWaveRunning;
    private bool _hasSpawnedInWave;
    private float _nextWaveTimer;

    public IReadOnlyList<Enemy> AliveEnemies => _aliveEnemies;
    public Transform PlayerTransform => _playerTransform;
    public int CurrentEnemyCount => _aliveEnemies.Count;
    public int RemainingEnemyCount => GetRemainingEnemyCount();
    public int TotalDefeatedCount => _totalDefeatedCount;
    public int CurrentWaveIndex => _currentWaveIndex;
    public bool IsWaveRunning => _isWaveRunning;
    public int MaxAliveCount => GetCurrentMaxAliveCount();
    public bool CanSpawnEnemy => CurrentEnemyCount < MaxAliveCount;
    public UnityEvent OnAllEnemiesDefeated => _onAllEnemiesDefeated;
    public EnemyEvent OnEnemyDied => _onEnemyDied;
    public WaveEvent OnWaveStarted => _onWaveStarted;
    public WaveEvent OnWaveEnded => _onWaveEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeReferences();
    }

    private void Start()
    {
        StopAllSpawners();

        if (_startWaveOnStart)
        {
            StartWave(0);
        }
    }

    private void Update()
    {
        RemoveNullEnemies();

        if (_isWaveRunning)
        {
            UpdateWaveClearCheck();
            return;
        }

        if (_nextWaveTimer <= 0.0f)
        {
            return;
        }

        _nextWaveTimer -= Time.deltaTime;

        if (_nextWaveTimer <= 0.0f)
        {
            StartWave(_currentWaveIndex + 1);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterEnemy(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        RegisterEnemy(enemyObject.GetComponent<Enemy>());
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || _aliveEnemies.Contains(enemy))
        {
            return;
        }

        _aliveEnemies.Add(enemy);
        _hasSpawnedInWave = true;
        enemy.SetTarget(_playerTransform);
        enemy.Died += HandleEnemyDied;
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.Died -= HandleEnemyDied;
        _aliveEnemies.Remove(enemy);
    }

    public void StartWave(int waveIndex)
    {
        if (waveIndex < 0)
        {
            return;
        }

        if (_waves.Count > 0 && waveIndex >= _waves.Count)
        {
            _onAllEnemiesDefeated.Invoke();
            return;
        }

        _currentWaveIndex = waveIndex;
        _defeatedCountInWave = 0;
        _hasSpawnedInWave = false;
        _isWaveRunning = true;
        _nextWaveTimer = 0.0f;

        StopAllSpawners();
        StartCurrentWaveSpawners();
        _onWaveStarted.Invoke(_currentWaveIndex);
    }

    public void EndCurrentWave()
    {
        if (!_isWaveRunning)
        {
            return;
        }

        _isWaveRunning = false;
        StopAllSpawners();
        _onWaveEnded.Invoke(_currentWaveIndex);

        if (_waves.Count == 0 || _currentWaveIndex + 1 >= _waves.Count)
        {
            _onAllEnemiesDefeated.Invoke();
        }
        else
        {
            _nextWaveTimer = _nextWaveDelay;
        }
    }

    public void ClearAllEnemies()
    {
        _isWaveRunning = false;
        _nextWaveTimer = 0.0f;
        StopAllSpawners();

        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _aliveEnemies[i];
            UnregisterEnemy(enemy);

            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        _aliveEnemies.Clear();
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;

        foreach (Enemy enemy in _aliveEnemies)
        {
            if (enemy != null)
            {
                enemy.SetTarget(_playerTransform);
            }
        }
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

        return ContainsSpawner(_spawners, spawner);
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        UnregisterEnemy(enemy);
        _defeatedCountInWave++;
        _totalDefeatedCount++;
        _onEnemyDied.Invoke(enemy);
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

        if (_hasSpawnedInWave && CurrentEnemyCount <= 0)
        {
            EndCurrentWave();
        }
    }

    private void InitializeReferences()
    {
        if (_playerTransform == null && _autoFindPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        if ((_spawners == null || _spawners.Length == 0) && _autoFindSpawners)
        {
            _spawners = FindObjectsOfType<EnemySpawner>();
        }
    }

    private void StopAllSpawners()
    {
        if (_spawners == null)
        {
            return;
        }

        foreach (EnemySpawner spawner in _spawners)
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
            StartSpawners(_spawners);
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
        return wave != null ? wave.MaxAliveCount : _maxAliveCount;
    }

    private int GetRemainingEnemyCount()
    {
        EnemyWave wave = GetCurrentWave();

        if (wave == null || wave.TargetDefeatCount <= 0)
        {
            return CurrentEnemyCount;
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

    private bool ContainsSpawner(IReadOnlyList<EnemySpawner> spawners, EnemySpawner target)
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

    private void RemoveNullEnemies()
    {
        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (_aliveEnemies[i] == null)
            {
                _aliveEnemies.RemoveAt(i);
            }
        }
    }
}
