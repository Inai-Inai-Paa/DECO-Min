using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵を条件に応じて生成し、必要に応じてEnemyManagerへ登録するクラス
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    private enum SpawnPositionType
    {
        SpawnPoints,
        Area,
        RandomAroundSpawner
    }

    private enum SpawnCondition
    {
        TimeElapsed,
        WaveStart,
        EnemyCountBelow
    }

    [Header("敵Prefab指定")]
    [SerializeField] private GameObject _enemyPrefab;

    [Header("Spawn位置指定")]
    [SerializeField] private SpawnPositionType _spawnPositionType = SpawnPositionType.SpawnPoints;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Vector3 _areaCenterOffset = Vector3.zero;
    [SerializeField] private Vector3 _areaSize = new Vector3(10.0f, 0.0f, 10.0f);
    [SerializeField] private float _randomRadius = 8.0f;
    [SerializeField] private int _positionSearchCount = 20;

    [Header("Spawn条件")]
    [SerializeField] private SpawnCondition _spawnCondition = SpawnCondition.TimeElapsed;
    [SerializeField] private bool _spawnOnStart = true;
    [SerializeField] private int _spawnWhenAliveCountBelow = 1;

    [Header("Spawn間隔・数")]
    [SerializeField] private float _spawnInterval = 5.0f;
    [SerializeField] private int _spawnCount = 1;
    [SerializeField] private int _maxSpawnCount = 10;
    [SerializeField] private int _maxAliveCount = 3;

    [Header("同時出現制限")]
    [SerializeField] private float _aliveCheckRadius = 12.0f;

    [Header("Area Spawn")]
    [SerializeField, Min(0.0f)] private float _entrySpawnWaitTime = 0.25f;
    [SerializeField, Min(0.0f)] private float _respawnCooldown = 3.0f;

    [Header("Spawn前演出")]
    [SerializeField] private GameObject _warningMarkerPrefab;
    [SerializeField] private GameObject _spawnEffectPrefab;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _spawnSound;
    [SerializeField] private float _spawnDelay = 0.0f;

    [Header("Spawn禁止判定")]
    [SerializeField] private Transform _player;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _minPlayerDistance = 4.0f;

    [Header("NavMesh上補正")]
    [SerializeField] private bool _snapToNavMesh = true;
    [SerializeField] private float _navMeshSearchDistance = 3.0f;
    [SerializeField] private int _navMeshAreaMask = NavMesh.AllAreas;
    [SerializeField, Min(0.0f)] private float _spawnOverlapRadius = 0.6f;
    [SerializeField] private LayerMask _spawnBlockLayerMask = ~0;

    [Header("Managerへ登録")]
    [SerializeField] private EnemyManager _enemyManager;

    private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
    private readonly List<GameObject> _activeWarningMarkers = new List<GameObject>();
    private Enemy _spawnedEnemy;
    private EnemyEncounterArea _spawnArea;
    private float _spawnTimer;
    private float _entrySpawnWaitTimer;
    private float _respawnCooldownTimer;
    private int _spawnedCount;
    private bool _isWaveActive;
    private bool _isSpawning;

    public int SpawnedCount => _spawnedCount;
    public int AliveCount
    {
        get
        {
            RemoveNullEnemies();
            return _spawnedEnemies.Count;
        }
    }

    private void Start()
    {
        CachePlayer();
        CacheEnemyManager();

        if (_spawnOnStart)
        {
            if (_spawnCondition == SpawnCondition.WaveStart)
            {
                StartWave();
                return;
            }

            TrySpawn();
        }
    }

    private void Update()
    {
        RemoveNullEnemies();
        UpdateRespawnCooldown();

        if (!UpdateAreaEntryWait())
        {
            return;
        }

        if (!CanUseCondition())
        {
            return;
        }

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        _spawnTimer = 0.0f;
        TrySpawn();
    }

    public void StartWave()
    {
        _isWaveActive = true;
        _spawnTimer = _spawnInterval;
    }

    public void StopWave()
    {
        _isWaveActive = false;
    }

    public void SpawnEnemy()
    {
        TrySpawn();
    }

    public void DespawnSpawnedEnemies()
    {
        StopAllCoroutines();
        _isSpawning = false;
        DestroyActiveWarningMarkers();

        EnemyManager enemyManager = GetEnemyManager();
        int despawnedCount = 0;

        for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemyObject = _spawnedEnemies[i];

            if (enemyObject == null)
            {
                _spawnedEnemies.RemoveAt(i);
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemyManager != null && enemy != null)
            {
                enemyManager.UnregisterEnemy(enemy);
            }

            _spawnedEnemies.RemoveAt(i);

            if (enemy != null)
            {
                enemy.Despawn();
            }
            else
            {
                Destroy(enemyObject);
            }

            despawnedCount++;
        }

        _spawnedCount = Mathf.Max(0, _spawnedCount - despawnedCount);
        _spawnTimer = _spawnInterval;
        StartRespawnCooldown();
    }

    public void RequestReturnSpawnedEnemy()
    {
        RemoveNullEnemies();

        if (_spawnedEnemy != null)
        {
            _spawnedEnemy.BeginReturnToHome();
        }
    }

    public void SetSpawnArea(EnemyEncounterArea spawnArea)
    {
        _spawnArea = spawnArea;
    }

    public bool IsPlayerInSpawnArea(Transform player)
    {
        if (_spawnArea == null)
        {
            return true;
        }

        return _spawnArea.IsPlayerInArea(player);
    }

    public void NotifyEnemyRemoved(Enemy enemy)
    {
        if (enemy == null || _spawnedEnemy != enemy)
        {
            return;
        }

        enemy.Removed -= NotifyEnemyRemoved;
        _spawnedEnemy = null;
        RemoveNullEnemies();
        StartRespawnCooldown();
    }

    private void TrySpawn()
    {
        RemoveNullEnemies();

        if (_isSpawning
            || _enemyPrefab == null
            || _spawnedEnemy != null
            || IsRespawnCoolingDown()
            || !HasPassedAreaEntryWait()
            || HasReachedMaxSpawnCount()
            || !CanUseEnemyManager())
        {
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        _isSpawning = true;

        int spawnableCount = GetSpawnableCount();

        for (int i = 0; i < spawnableCount; i++)
        {
            if (!CanSpawnMoreAliveEnemies() || !TryGetSpawnPose(out Vector3 position, out Quaternion rotation))
            {
                break;
            }

            GameObject marker = null;

            if (_warningMarkerPrefab != null)
            {
                marker = Instantiate(_warningMarkerPrefab, position, rotation);
                _activeWarningMarkers.Add(marker);
            }

            if (_spawnDelay > 0.0f)
            {
                yield return new WaitForSeconds(_spawnDelay);
            }

            if (marker != null)
            {
                Destroy(marker);
                _activeWarningMarkers.Remove(marker);
            }

            if (!CanSpawnMoreAliveEnemies())
            {
                break;
            }

            SpawnAt(position, rotation);
        }

        _isSpawning = false;
    }

    private void SpawnAt(Vector3 position, Quaternion rotation)
    {
        GameObject enemy = Instantiate(_enemyPrefab, position, rotation);
        Enemy enemyComponent = enemy.GetComponent<Enemy>();

        if (enemyComponent != null)
        {
            enemyComponent.SetSpawnPose(position, rotation);
            enemyComponent.SetSpawnOwner(this);
            enemyComponent.Removed += NotifyEnemyRemoved;
            _spawnedEnemy = enemyComponent;
        }

        _spawnedEnemies.Add(enemy);
        _spawnedCount++;

        if (_spawnEffectPrefab != null)
        {
            Instantiate(_spawnEffectPrefab, position, rotation);
        }

        if (_audioSource != null && _spawnSound != null)
        {
            _audioSource.PlayOneShot(_spawnSound);
        }

        RegisterEnemy(enemy);
    }

    private bool CanUseCondition()
    {
        if (_spawnArea != null && !_spawnArea.HasPlayersInArea)
        {
            return false;
        }

        if (HasReachedMaxSpawnCount())
        {
            return false;
        }

        if (!CanUseEnemyManager())
        {
            return false;
        }

        switch (_spawnCondition)
        {
            case SpawnCondition.TimeElapsed:
                return true;
            case SpawnCondition.WaveStart:
                return _isWaveActive;
            case SpawnCondition.EnemyCountBelow:
                return AliveCount < _spawnWhenAliveCountBelow;
            default:
                return false;
        }
    }

    private bool HasReachedMaxSpawnCount()
    {
        return _maxSpawnCount > 0 && _spawnedCount >= _maxSpawnCount;
    }

    private int GetSpawnableCount()
    {
        int requestedCount = Mathf.Max(1, _spawnCount);

        if (_maxSpawnCount <= 0)
        {
            return requestedCount;
        }

        return Mathf.Min(requestedCount, _maxSpawnCount - _spawnedCount);
    }

    private bool CanSpawnMoreAliveEnemies()
    {
        if (_spawnedEnemy != null)
        {
            return false;
        }

        EnemyManager enemyManager = GetEnemyManager();

        if (enemyManager != null && !enemyManager.CanSpawnEnemy)
        {
            return false;
        }

        if (_maxAliveCount <= 0)
        {
            return true;
        }

        return AliveCount < _maxAliveCount && CountAliveEnemiesAroundSpawner() < _maxAliveCount;
    }

    private int CountAliveEnemiesAroundSpawner()
    {
        int count = 0;

        foreach (GameObject enemy in _spawnedEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= _aliveCheckRadius)
            {
                count++;
            }
        }

        return count;
    }

    private bool TryGetSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        int searchCount = Mathf.Max(1, _positionSearchCount);

        for (int i = 0; i < searchCount; i++)
        {
            position = GetRawSpawnPosition(out rotation);

            if (_snapToNavMesh && !TrySnapToNavMesh(position, out position))
            {
                continue;
            }

            if (IsTooCloseToPlayer(position))
            {
                continue;
            }

            if (IsSpawnPositionBlocked(position))
            {
                continue;
            }

            return true;
        }

        position = transform.position;
        rotation = transform.rotation;
        return false;
    }

    private Vector3 GetRawSpawnPosition(out Quaternion rotation)
    {
        rotation = transform.rotation;

        switch (_spawnPositionType)
        {
            case SpawnPositionType.SpawnPoints:
                return GetSpawnPointPosition(out rotation);
            case SpawnPositionType.Area:
                return GetRandomAreaPosition();
            case SpawnPositionType.RandomAroundSpawner:
                return GetRandomAroundSpawnerPosition();
            default:
                return transform.position;
        }
    }

    private Vector3 GetSpawnPointPosition(out Quaternion rotation)
    {
        rotation = transform.rotation;

        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            return transform.position;
        }

        Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        if (spawnPoint == null)
        {
            return transform.position;
        }

        rotation = spawnPoint.rotation;
        return spawnPoint.position;
    }

    private Vector3 GetRandomAreaPosition()
    {
        Vector3 center = transform.position + _areaCenterOffset;
        float x = Random.Range(-_areaSize.x * 0.5f, _areaSize.x * 0.5f);
        float z = Random.Range(-_areaSize.z * 0.5f, _areaSize.z * 0.5f);

        return center + new Vector3(x, 0.0f, z);
    }

    private Vector3 GetRandomAroundSpawnerPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * _randomRadius;
        return transform.position + new Vector3(randomCircle.x, 0.0f, randomCircle.y);
    }

    private bool TrySnapToNavMesh(Vector3 sourcePosition, out Vector3 snappedPosition)
    {
        if (NavMesh.SamplePosition(sourcePosition, out NavMeshHit hit, _navMeshSearchDistance, _navMeshAreaMask))
        {
            snappedPosition = hit.position;
            return true;
        }

        snappedPosition = sourcePosition;
        return false;
    }

    private bool IsSpawnPositionBlocked(Vector3 position)
    {
        if (_spawnOverlapRadius <= 0.0f)
        {
            return false;
        }

        return Physics.CheckSphere(
            position + Vector3.up * _spawnOverlapRadius,
            _spawnOverlapRadius,
            _spawnBlockLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool IsTooCloseToPlayer(Vector3 position)
    {
        CachePlayer();

        if (_player == null || _minPlayerDistance <= 0.0f)
        {
            return false;
        }

        return Vector3.Distance(position, _player.position) < _minPlayerDistance;
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

    private void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        EnemyManager enemyManager = GetEnemyManager();

        if (enemyManager != null)
        {
            enemyManager.RegisterEnemy(enemy);
        }
    }

    private bool CanUseEnemyManager()
    {
        EnemyManager enemyManager = GetEnemyManager();

        if (enemyManager == null)
        {
            return true;
        }

        return enemyManager.IsWaveRunning
            && enemyManager.CanSpawnEnemy
            && enemyManager.ContainsSpawner(this);
    }

    private EnemyManager GetEnemyManager()
    {
        if (_enemyManager != null)
        {
            return _enemyManager;
        }

        return EnemyManager.Instance;
    }

    private void CacheEnemyManager()
    {
        if (_enemyManager == null)
        {
            _enemyManager = EnemyManager.Instance;
        }
    }

    private void DestroyActiveWarningMarkers()
    {
        for (int i = _activeWarningMarkers.Count - 1; i >= 0; i--)
        {
            GameObject marker = _activeWarningMarkers[i];

            if (marker != null)
            {
                Destroy(marker);
            }

            _activeWarningMarkers.RemoveAt(i);
        }
    }

    private void RemoveNullEnemies()
    {
        for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (_spawnedEnemies[i] == null)
            {
                _spawnedEnemies.RemoveAt(i);
            }
        }

        if (_spawnedEnemy != null && !_spawnedEnemies.Contains(_spawnedEnemy.gameObject))
        {
            _spawnedEnemy = null;
        }
    }

    private bool UpdateAreaEntryWait()
    {
        if (_spawnArea == null)
        {
            return true;
        }

        if (!_spawnArea.HasPlayersInArea)
        {
            _entrySpawnWaitTimer = 0.0f;
            return false;
        }

        _entrySpawnWaitTimer += Time.deltaTime;
        return _entrySpawnWaitTimer >= _entrySpawnWaitTime;
    }

    private void UpdateRespawnCooldown()
    {
        if (_respawnCooldownTimer <= 0.0f)
        {
            return;
        }

        _respawnCooldownTimer = Mathf.Max(0.0f, _respawnCooldownTimer - Time.deltaTime);
    }

    private bool IsRespawnCoolingDown()
    {
        return _respawnCooldownTimer > 0.0f;
    }

    private bool HasPassedAreaEntryWait()
    {
        return _spawnArea == null || _entrySpawnWaitTimer >= _entrySpawnWaitTime;
    }

    private void StartRespawnCooldown()
    {
        _respawnCooldownTimer = Mathf.Max(_respawnCooldownTimer, _respawnCooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _aliveCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _minPlayerDistance);

        Gizmos.color = Color.green;

        if (_spawnPositionType == SpawnPositionType.Area)
        {
            Gizmos.DrawWireCube(transform.position + _areaCenterOffset, _areaSize);
        }
        else if (_spawnPositionType == SpawnPositionType.RandomAroundSpawner)
        {
            Gizmos.DrawWireSphere(transform.position, _randomRadius);
        }
    }
}
