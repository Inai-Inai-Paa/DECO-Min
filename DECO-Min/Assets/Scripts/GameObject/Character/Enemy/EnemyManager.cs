using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared settings and lifecycle for EnemyPopulationArea groups.
/// Each area owns its spawn count and formation. This manager applies
/// shared cooldowns, resets groups when the player dies or a scene loads,
/// and raises group events.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public const float DefaultGroupWipeRespawnSeconds = 300.0f;
    public const float DefaultDespawnDelaySeconds = 10.0f;
    public const float DefaultChaseOutsideSeconds = 10.0f;

    [Serializable]
    public class EnemyEvent : UnityEvent<Enemy>
    {
    }

    public static EnemyManager Instance { get; private set; }

    [Header("参照")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private EnemyPopulationArea[] _areas;
    [SerializeField] private bool _autoFindPlayer = true;
    [SerializeField] private bool _autoFindAreas = true;

    [Header("再出現")]
    [SerializeField, Min(0.0f)] private float _groupWipeRespawnSeconds = DefaultGroupWipeRespawnSeconds;
    [SerializeField, Min(0.0f)] private float _despawnDelaySeconds = DefaultDespawnDelaySeconds;
    [SerializeField, Min(0.0f)] private float _chaseOutsideSeconds = DefaultChaseOutsideSeconds;

    [Header("リセット")]
    [SerializeField] private bool _resetWhenPlayerDies = true;
    [SerializeField] private UnityEvent _onPopulationReset = new UnityEvent();

    [Header("通知")]
    [SerializeField] private EnemyEvent _onEnemyDefeated = new EnemyEvent();
    [SerializeField] private UnityEvent _onGroupWiped = new UnityEvent();
    [SerializeField] private UnityEvent _onGroupSpawned = new UnityEvent();
    [SerializeField] private UnityEvent _onGroupDespawned = new UnityEvent();

    private readonly List<EnemyPopulationArea> _runtimeAreas = new List<EnemyPopulationArea>();
    private bool _playerDeathConsumed;
    private int _enableFrame;

    public Transform PlayerTransform => _playerTransform;
    public float GroupWipeRespawnSeconds => _groupWipeRespawnSeconds;
    public float DespawnDelaySeconds => _despawnDelaySeconds;
    public float ChaseOutsideSeconds => _chaseOutsideSeconds;
    public IReadOnlyList<EnemyPopulationArea> Areas => _runtimeAreas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CachePlayer();
    }

    private void OnEnable()
    {
        _enableFrame = Time.frameCount;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        CollectAreas();
    }

    private void Update()
    {
        TickPlayerDeath();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterArea(EnemyPopulationArea area)
    {
        if (area == null || _runtimeAreas.Contains(area))
        {
            return;
        }

        _runtimeAreas.Add(area);
    }

    public void UnregisterArea(EnemyPopulationArea area)
    {
        _runtimeAreas.Remove(area);
    }

    public void NotifyEnemyDefeated(Enemy enemy)
    {
        _onEnemyDefeated.Invoke(enemy);
    }

    public void NotifyGroupWiped()
    {
        _onGroupWiped.Invoke();
    }

    public void NotifyGroupSpawned()
    {
        _onGroupSpawned.Invoke();
    }

    public void NotifyGroupDespawned()
    {
        _onGroupDespawned.Invoke();
    }

    public void NotifyPlayerDied()
    {
        if (_playerDeathConsumed)
        {
            return;
        }

        _playerDeathConsumed = true;
        ResetPopulation();
    }

    public void ResetPopulation()
    {
        for (int i = _runtimeAreas.Count - 1; i >= 0; i--)
        {
            EnemyPopulationArea area = _runtimeAreas[i];

            if (area == null)
            {
                _runtimeAreas.RemoveAt(i);
                continue;
            }

            area.ResetForRestart();
        }

        _onPopulationReset.Invoke();
    }

    private void TickPlayerDeath()
    {
        if (!_resetWhenPlayerDies)
        {
            return;
        }

        CachePlayer();

        if (_playerTransform == null)
        {
            return;
        }

        Player player = _playerTransform.GetComponent<Player>();

        if (player == null || player.Status == null)
        {
            return;
        }

        if (player.Status.currentHealth > 0.0f)
        {
            _playerDeathConsumed = false;
            return;
        }

        NotifyPlayerDied();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Time.frameCount == _enableFrame)
        {
            return;
        }

        _playerDeathConsumed = false;
        CachePlayer();
        ResetPopulation();
    }

    private void CollectAreas()
    {
        if (_areas != null)
        {
            for (int i = 0; i < _areas.Length; i++)
            {
                RegisterArea(_areas[i]);
            }
        }

        if (!_autoFindAreas)
        {
            return;
        }

        EnemyPopulationArea[] found = FindObjectsByType<EnemyPopulationArea>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < found.Length; i++)
        {
            RegisterArea(found[i]);
        }
    }

    private void CachePlayer()
    {
        if (_playerTransform != null || !_autoFindPlayer)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }
}
