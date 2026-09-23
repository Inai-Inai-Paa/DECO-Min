using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawnArea : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Enemy[] _enemies;
    [SerializeField, Min(0.1f)] private float _spawnRadius = 20.0f;
    [SerializeField, Min(0.0f)] private float _spawnDelay = 0.0f;

    [Header("Leash Settings")]
    [SerializeField, Min(0.1f)] private float _leashRadius = 30.0f;
    [SerializeField, Min(0.1f)] private float _returnCompleteDistance = 1.0f;
    [SerializeField] private bool _resumeCombatDuringReturn = true;
    [SerializeField, Min(0.05f)] private float _leashCheckInterval = 0.2f;
    [SerializeField, Min(0.0f)] private float _returnMoveSpeed = 0.0f;

    [Header("Despawn Settings")]
    [SerializeField, Min(0.0f)] private float _despawnDelay = 5.0f;

    [Header("Reset Settings")]
    [SerializeField] private bool _resetHPOnReturn = true;

    [Header("Debug Settings")]
    [SerializeField] private bool _showSpawnArea = true;
    [SerializeField] private bool _showLeashArea = true;

    [SerializeField] private string _playerTag = "Player";

    private Transform _player;
    private float _spawnTimer;
    private bool _spawnRequested;
    private bool _hasSpawned;

    public Transform Player => _player;
    public float DespawnDelay => _despawnDelay;
    public float ReturnCompleteDistance => _returnCompleteDistance;
    public float ReturnMoveSpeed => _returnMoveSpeed;
    public float LeashCheckInterval => _leashCheckInterval;
    public bool ResumeCombatDuringReturn => _resumeCombatDuringReturn;
    public bool ResetHPOnReturn => _resetHPOnReturn;

    private void Awake()
    {
        if (_spawnPoint == null)
        {
            _spawnPoint = transform;
        }

        if (_leashRadius < _spawnRadius)
        {
            _leashRadius = _spawnRadius;
        }

        BindEnemies(false);
    }

    private void Update()
    {
        CachePlayer();

        if (!IsPlayerInsideSpawn())
        {
            _spawnRequested = false;
            _spawnTimer = 0.0f;
            return;
        }

        if (_enemies == null)
        {
            return;
        }

        if (_hasSpawned && !HasActiveEnemy())
        {
            _hasSpawned = false;
        }

        if (_hasSpawned)
        {
            return;
        }

        if (!_spawnRequested)
        {
            _spawnRequested = true;
            _spawnTimer = 0.0f;
        }

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < _spawnDelay)
        {
            return;
        }

        ActivateEnemies();
    }

    public bool IsPlayerInsideSpawn()
    {
        CachePlayer();

        if (_player == null)
        {
            return false;
        }

        return Vector3.Distance(transform.position, _player.position) <= _spawnRadius;
    }

    public bool IsInsideLeash(Vector3 position)
    {
        return Vector3.Distance(transform.position, position) <= _leashRadius;
    }

    public void NotifyEnemyDeactivated()
    {
        if (_enemies == null)
        {
            _hasSpawned = false;
            return;
        }

        for (int i = 0; i < _enemies.Length; i++)
        {
            Enemy enemy = _enemies[i];

            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.Lifecycle != EnemyLifecycle.Inactive)
            {
                return;
            }
        }

        _hasSpawned = false;
        _spawnRequested = false;
        _spawnTimer = 0.0f;
    }

    private void ActivateEnemies()
    {
        _spawnRequested = false;
        int activatedCount = 0;

        for (int i = 0; i < _enemies.Length; i++)
        {
            Enemy enemy = _enemies[i];

            if (enemy == null || enemy.Lifecycle == EnemyLifecycle.Dead)
            {
                continue;
            }

            if (enemy.gameObject.activeInHierarchy
                && enemy.Lifecycle != EnemyLifecycle.Inactive
                && enemy.Lifecycle != EnemyLifecycle.WaitDespawn)
            {
                activatedCount++;
                continue;
            }

            enemy.ActivateAtSpawn(_player);
            activatedCount++;
        }

        _hasSpawned = activatedCount > 0;
    }

    private void BindEnemies(bool activate)
    {
        if (_enemies == null)
        {
            return;
        }

        for (int i = 0; i < _enemies.Length; i++)
        {
            Enemy enemy = _enemies[i];

            if (enemy == null)
            {
                continue;
            }

            enemy.BindSpawnArea(this);
            enemy.SetSpawnPose(_spawnPoint.position, _spawnPoint.rotation);

            if (!activate)
            {
                enemy.PrepareInactive();
            }
        }
    }

    private bool HasActiveEnemy()
    {
        for (int i = 0; i < _enemies.Length; i++)
        {
            Enemy enemy = _enemies[i];

            if (enemy != null
                && enemy.gameObject.activeInHierarchy
                && enemy.Lifecycle != EnemyLifecycle.Inactive
                && enemy.Lifecycle != EnemyLifecycle.Dead)
            {
                return true;
            }
        }

        return false;
    }

    private void CachePlayer()
    {
        if (_player != null || string.IsNullOrEmpty(_playerTag))
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
    }

    private void OnValidate()
    {
        if (_leashRadius < _spawnRadius)
        {
            _leashRadius = _spawnRadius;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_showSpawnArea)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }

        if (_showLeashArea)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _leashRadius);
        }

        Transform point = _spawnPoint != null ? _spawnPoint : transform;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(point.position, _returnCompleteDistance);
        Gizmos.DrawLine(transform.position, point.position);
    }
}
