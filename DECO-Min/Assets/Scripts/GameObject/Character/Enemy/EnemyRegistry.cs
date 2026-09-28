using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks alive enemies and applies shared targeting.
/// </summary>
public class EnemyRegistry
{
    private readonly List<Enemy> _aliveEnemies = new List<Enemy>();
    private Transform _playerTransform;

    public event Action<Enemy> EnemyDied;

    public IReadOnlyList<Enemy> AliveEnemies => _aliveEnemies;
    public int Count => _aliveEnemies.Count;
    public Transform PlayerTransform => _playerTransform;

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

    public bool Register(Enemy enemy)
    {
        if (enemy == null || _aliveEnemies.Contains(enemy))
        {
            return false;
        }

        _aliveEnemies.Add(enemy);
        enemy.SetTarget(_playerTransform);
        enemy.Died += HandleEnemyDied;
        return true;
    }

    public void Unregister(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.Died -= HandleEnemyDied;
        _aliveEnemies.Remove(enemy);
    }

    public void ClearAll()
    {
        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _aliveEnemies[i];
            Unregister(enemy);

            if (enemy != null)
            {
                UnityEngine.Object.Destroy(enemy.gameObject);
            }
        }

        _aliveEnemies.Clear();
    }

    public void RemoveNullEnemies()
    {
        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (_aliveEnemies[i] == null)
            {
                _aliveEnemies.RemoveAt(i);
            }
        }
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        Unregister(enemy);
        EnemyDied?.Invoke(enemy);
    }
}
