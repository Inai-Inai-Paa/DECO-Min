using System;
using UnityEngine;

public enum EnemyBulletFlight
{
    Direct,
    Arc
}

public class EnemyBullet : MonoBehaviour
{
    [SerializeField]
    private EnemyBulletFlight _flight = EnemyBulletFlight.Direct;
    [SerializeField]
    private float _bulletSpeed = 12.0f;
    [SerializeField]
    private float _gravity = 12.0f;
    [SerializeField]
    private int _bulletDamage = 10;
    [SerializeField]
    private float _bulletLifeTime = 5.0f;
    [SerializeField]
    private LayerMask _obstacleMask = ~0;

    private Vector3 _velocity = Vector3.forward;
    private Enemy _owner;
    private float _aliveTimer;
    private bool _hasHit;
    private bool _useGravity;

    public bool LaunchAt(Vector3 targetPoint, Enemy owner)
    {
        _owner = owner;
        _aliveTimer = 0.0f;
        _hasHit = false;

        if (_flight == EnemyBulletFlight.Arc)
        {
            if (!TryGetArcVelocity(transform.position, targetPoint, _bulletSpeed, _gravity, out _velocity))
            {
                return false;
            }

            _useGravity = true;
        }
        else
        {
            Vector3 direction = targetPoint - transform.position;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
            }

            _velocity = direction.normalized * Mathf.Max(0.01f, _bulletSpeed);
            _useGravity = false;
        }

        if (_velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(_velocity.normalized);
        }

        return true;
    }

    public static bool TryGetArcVelocity(Vector3 start, Vector3 end, float speed, float gravity, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        float g = Mathf.Abs(gravity);

        if (speed <= 0.01f || g <= 0.01f)
        {
            return false;
        }

        Vector3 delta = end - start;
        Vector3 deltaXZ = new Vector3(delta.x, 0.0f, delta.z);
        float xz = deltaXZ.magnitude;
        float y = delta.y;
        float speedSqr = speed * speed;
        float discriminant = speedSqr * speedSqr - g * (g * xz * xz + 2.0f * y * speedSqr);

        if (discriminant < 0.0f || xz <= 0.001f)
        {
            return false;
        }

        float tanAngle = (speedSqr - Mathf.Sqrt(discriminant)) / (g * xz);
        float angle = Mathf.Atan(tanAngle);
        Vector3 directionXZ = deltaXZ / xz;
        velocity = directionXZ * (Mathf.Cos(angle) * speed) + Vector3.up * (Mathf.Sin(angle) * speed);
        return velocity.sqrMagnitude > 0.0001f;
    }

    private void Update()
    {
        if (_hasHit)
        {
            return;
        }

        _aliveTimer += Time.deltaTime;

        if (_aliveTimer >= _bulletLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        float deltaTime = Time.deltaTime;

        if (_useGravity)
        {
            _velocity += Vector3.down * Mathf.Abs(_gravity) * deltaTime;
        }

        Vector3 delta = _velocity * deltaTime;

        if (!TryMove(delta))
        {
            return;
        }

        if (delta.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(delta.normalized);
        }
    }

    private bool TryMove(Vector3 delta)
    {
        float distance = delta.magnitude;

        if (distance <= 0.0001f)
        {
            return true;
        }

        Vector3 direction = delta / distance;
        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            direction,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsOwnedBySource(hit.collider))
            {
                continue;
            }

            if (TryApplyDamage(hit.collider))
            {
                _hasHit = true;
                Destroy(gameObject);
                return false;
            }

            if (IsObstacle(hit.collider))
            {
                Destroy(gameObject);
                return false;
            }
        }

        transform.position += delta;
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit || other == null || IsOwnedBySource(other))
        {
            return;
        }

        if (TryApplyDamage(other))
        {
            _hasHit = true;
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger && IsObstacle(other))
        {
            Destroy(gameObject);
        }
    }

    private bool IsObstacle(Collider other)
    {
        return (_obstacleMask.value & (1 << other.gameObject.layer)) != 0;
    }

    private bool IsOwnedBySource(Collider other)
    {
        if (_owner == null)
        {
            return false;
        }

        if (other.transform == _owner.transform || other.transform.IsChildOf(_owner.transform))
        {
            return true;
        }

        Enemy hitEnemy = other.GetComponent<Enemy>() ?? other.GetComponentInParent<Enemy>();
        return hitEnemy == _owner;
    }

    private bool TryApplyDamage(Collider other)
    {
        Player player = other.GetComponent<Player>() ?? other.GetComponentInParent<Player>();

        if (player != null)
        {
            player.TryDamage(_bulletDamage);
            return true;
        }

        IDamageable damageable = other.GetComponent<IDamageable>()
            ?? other.GetComponentInParent<IDamageable>();

        if (damageable == null || ReferenceEquals(damageable, _owner))
        {
            return false;
        }

        damageable.TakeDamage(_bulletDamage);
        return true;
    }
}
