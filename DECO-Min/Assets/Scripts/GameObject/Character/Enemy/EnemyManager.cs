using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Facade for enemy registry and wave progression.
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

    private readonly EnemyRegistry _registry = new EnemyRegistry();
    private readonly EnemyWaveController _waveController = new EnemyWaveController();

    public IReadOnlyList<Enemy> AliveEnemies => _registry.AliveEnemies;
    public Transform PlayerTransform => _registry.PlayerTransform;
    public int CurrentEnemyCount => _registry.Count;
    public int RemainingEnemyCount => _waveController.RemainingEnemyCount;
    public int TotalDefeatedCount => _waveController.TotalDefeatedCount;
    public int CurrentWaveIndex => _waveController.CurrentWaveIndex;
    public bool IsWaveRunning => _waveController.IsWaveRunning;
    public int MaxAliveCount => _waveController.MaxAliveCount;
    public bool CanSpawnEnemy => _waveController.CanSpawnEnemy;
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
        InitializeControllers();
    }

    private void Start()
    {
        _waveController.Stop();

        if (_startWaveOnStart)
        {
            StartWave(0);
        }
    }

    private void Update()
    {
        _registry.RemoveNullEnemies();
        _waveController.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        _waveController.Dispose();

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
        if (_registry.Register(enemy))
        {
            _waveController.NotifyEnemySpawned();
        }
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        _registry.Unregister(enemy);
    }

    public void StartWave(int waveIndex)
    {
        _waveController.StartWave(waveIndex);
    }

    public void EndCurrentWave()
    {
        _waveController.EndCurrentWave();
    }

    public void ClearAllEnemies()
    {
        _waveController.Stop();
        _registry.ClearAll();
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        _registry.SetPlayerTransform(playerTransform);
        _playerTransform = playerTransform;
    }

    public bool ContainsSpawner(EnemySpawner spawner)
    {
        return _waveController.ContainsSpawner(spawner);
    }

    private void InitializeControllers()
    {
        _registry.SetPlayerTransform(_playerTransform);
        _waveController.Initialize(
            _waves,
            _spawners,
            _registry,
            _nextWaveDelay,
            _maxAliveCount);

        _waveController.WaveStarted += waveIndex => _onWaveStarted.Invoke(waveIndex);
        _waveController.WaveEnded += waveIndex => _onWaveEnded.Invoke(waveIndex);
        _waveController.AllEnemiesDefeated += () => _onAllEnemiesDefeated.Invoke();
        _waveController.EnemyDiedInWave += enemy => _onEnemyDied.Invoke(enemy);
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
}
