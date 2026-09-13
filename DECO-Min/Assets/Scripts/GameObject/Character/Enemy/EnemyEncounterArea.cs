using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyEncounterArea : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private LayerMask _playerLayer;

    [Header("Spawner")]
    [SerializeField] private EnemySpawner[] _enemySpawners;
    [SerializeField] private bool _disableSpawnersUntilTriggered = true;
    [SerializeField] private bool _spawnImmediately = true;
    [SerializeField] private bool _activateOnce = true;
    [SerializeField] private bool _despawnWhenPlayerLeaves = true;
    [SerializeField, Min(0.0f)] private float _despawnDelay = 10.0f;

    [Header("Enemy Manager")]
    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private bool _startManagerWave;
    [SerializeField] private int _waveIndex;

    private readonly HashSet<GameObject> _playersInArea = new HashSet<GameObject>();
    private bool _hasActivated;
    private float _playerOutsideTimer;

    public bool HasPlayersInArea
    {
        get
        {
            RemoveMissingPlayers();
            return _playersInArea.Count > 0;
        }
    }

    private void Awake()
    {
        SetColliderAsTrigger();
        BindSpawners();

        if (_disableSpawnersUntilTriggered)
        {
            SetSpawnersEnabled(false);
        }
    }

    private void Update()
    {
        if (!_despawnWhenPlayerLeaves || !_hasActivated || IsPlayerInArea())
        {
            _playerOutsideTimer = 0.0f;
            return;
        }

        _playerOutsideTimer += Time.deltaTime;

        if (_playerOutsideTimer >= _despawnDelay)
        {
            DespawnAndWaitForPlayer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !IsInPlayerLayer(other.gameObject))
        {
            return;
        }

        _playersInArea.Add(GetPlayerRoot(other));
        _playerOutsideTimer = 0.0f;
        Activate();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || !IsInPlayerLayer(other.gameObject))
        {
            return;
        }

        _playersInArea.Remove(GetPlayerRoot(other));
    }

    public void Activate()
    {
        if (_activateOnce && _hasActivated)
        {
            return;
        }

        _hasActivated = true;
        SetSpawnersEnabled(true);

        if (_startManagerWave)
        {
            GetEnemyManager()?.StartWave(_waveIndex);
            return;
        }

        ActivateSpawners();
    }

    private void ActivateSpawners()
    {
        if (_enemySpawners == null)
        {
            return;
        }

        foreach (EnemySpawner spawner in _enemySpawners)
        {
            if (spawner == null)
            {
                continue;
            }

            spawner.StartWave();

            if (_spawnImmediately)
            {
                spawner.SpawnEnemy();
            }
        }
    }

    private void DespawnAndWaitForPlayer()
    {
        _playerOutsideTimer = 0.0f;

        if (_enemySpawners != null && _enemySpawners.Length > 0)
        {
            RequestSpawnerEnemiesReturn();
        }
        else if (_startManagerWave)
        {
            GetEnemyManager()?.ClearAllEnemies();
        }
    }

    private void RequestSpawnerEnemiesReturn()
    {
        if (_enemySpawners == null)
        {
            return;
        }

        foreach (EnemySpawner spawner in _enemySpawners)
        {
            if (spawner == null)
            {
                continue;
            }

            spawner.RequestReturnSpawnedEnemy();
        }
    }

    private void SetSpawnersEnabled(bool isEnabled)
    {
        if (_enemySpawners == null)
        {
            return;
        }

        foreach (EnemySpawner spawner in _enemySpawners)
        {
            if (spawner != null)
            {
                spawner.enabled = isEnabled;
            }
        }
    }

    private bool IsInPlayerLayer(GameObject target)
    {
        return (_playerLayer.value & (1 << target.layer)) != 0;
    }

    private bool IsPlayerInArea()
    {
        return HasPlayersInArea;
    }

    public bool IsPlayerInArea(Transform player)
    {
        RemoveMissingPlayers();

        if (player == null)
        {
            return HasPlayersInArea;
        }

        Transform current = player;

        while (current != null)
        {
            if (_playersInArea.Contains(current.gameObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private GameObject GetPlayerRoot(Collider playerCollider)
    {
        if (playerCollider.attachedRigidbody != null)
        {
            return playerCollider.attachedRigidbody.gameObject;
        }

        return playerCollider.gameObject;
    }

    private EnemyManager GetEnemyManager()
    {
        if (_enemyManager != null)
        {
            return _enemyManager;
        }

        return EnemyManager.Instance;
    }

    private void OnValidate()
    {
        SetColliderAsTrigger();
    }

    private void SetColliderAsTrigger()
    {
        Collider trigger = GetComponent<Collider>();

        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void BindSpawners()
    {
        if (_enemySpawners == null)
        {
            return;
        }

        foreach (EnemySpawner spawner in _enemySpawners)
        {
            if (spawner != null)
            {
                spawner.SetSpawnArea(this);
            }
        }
    }

    private void RemoveMissingPlayers()
    {
        _playersInArea.RemoveWhere(player => player == null);
    }
}
