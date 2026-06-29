using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵を指定地点から一定数までスポーンさせるクラス
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("スポーン設定")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private int _maxAliveCount = 3;
    [SerializeField] private float _spawnInterval = 5.0f;
    [SerializeField] private bool _spawnOnStart = true;

    private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();

    private float _spawnTimer;

    private void Start()
    {
        if (_spawnOnStart)
        {
            SpawnEnemy();
        }
    }

    private void Update()
    {
        RemoveNullEnemies();

        if (_spawnedEnemies.Count >= _maxAliveCount)
        {
            return;
        }

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0.0f;
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        if (_enemyPrefab == null || _spawnPoints == null || _spawnPoints.Length == 0)
        {
            return;
        }

        Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        GameObject enemy = Instantiate(
            _enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        _spawnedEnemies.Add(enemy);
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
    }
}