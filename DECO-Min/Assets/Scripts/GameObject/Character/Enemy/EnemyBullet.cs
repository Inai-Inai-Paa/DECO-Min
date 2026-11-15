using System;
using System.Collections.Generic;
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
    [Header("Area Drop")]
    [SerializeField, Min(0.0f)]
    private float _straightDistance = 5.0f;
    [SerializeField, Min(0.0f)]
    private float _dropBlendTime = 0.45f;
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
    private bool _timedArea;
    private Vector3 _launchPosition;
    private Vector3 _launchVelocity;
    private Vector3 _impactPoint;
    private float _flightTime;
    private float _straightTime;
    private float _dropBlend;
    private float _planGravity;
    private float _areaRadius;
    private int _areaDamage;

    public bool TryGetAreaLaunchVelocity(Vector3 start, Vector3 end, out Vector3 velocity)
    {
        if (!TryPlanAreaFlight(start, end, out AreaFlightPlan plan))
        {
            velocity = Vector3.zero;
            return false;
        }

        velocity = plan.InitialVelocity;
        return velocity.sqrMagnitude > 0.0001f;
    }

    public bool TryGetAreaFlightTimes(Vector3 start, Vector3 end, out float flightTime, out float straightTime)
    {
        if (!TryPlanAreaFlight(start, end, out AreaFlightPlan plan))
        {
            flightTime = 0.0f;
            straightTime = 0.0f;
            return false;
        }

        flightTime = plan.FlightTime;
        straightTime = plan.StraightTime;
        return true;
    }

    public bool TrySampleAreaFlight(Vector3 start, Vector3 end, float time, out Vector3 position)
    {
        if (!TryPlanAreaFlight(start, end, out AreaFlightPlan plan))
        {
            position = start;
            return false;
        }

        position = EvaluateAreaPosition(start, plan, time);
        return true;
    }

    public bool LaunchArea(Vector3 targetPoint, Enemy owner, float radius, int damage)
    {
        if (!TryPlanAreaFlight(transform.position, targetPoint, out AreaFlightPlan plan))
        {
            return false;
        }

        _owner = owner;
        _timedArea = true;
        _aliveTimer = 0.0f;
        _hasHit = false;
        _launchPosition = transform.position;
        _impactPoint = targetPoint;
        _flightTime = plan.FlightTime;
        _straightTime = plan.StraightTime;
        _dropBlend = plan.DropBlend;
        _planGravity = plan.Gravity;
        _areaRadius = Mathf.Max(0.01f, radius);
        _areaDamage = Mathf.Max(0, damage);
        _launchVelocity = plan.InitialVelocity;
        if (_launchVelocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(_launchVelocity.normalized);
        }

        return true;
    }

    public bool LaunchAt(Vector3 targetPoint, Enemy owner)
    {
        _timedArea = false;
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

        if (_timedArea)
        {
            AdvanceTimedFlight(Time.deltaTime);
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

            if (_timedArea)
            {
                if (hit.collider.GetComponentInParent<Enemy>() != null) continue;
                if (IsObstacle(hit.collider) || hit.collider.GetComponentInParent<Player>() != null)
                {
                    Explode(hit.point);
                    return false;
                }
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

        if (_timedArea)
        {
            if (other.GetComponentInParent<Enemy>() == null
                && ((!other.isTrigger && IsObstacle(other)) || other.GetComponentInParent<Player>() != null))
                Explode(transform.position);
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

    private void AdvanceTimedFlight(float deltaTime)
    {
        if (_hasHit) return;
        _aliveTimer = Mathf.Min(_flightTime, _aliveTimer + Mathf.Max(0, deltaTime));
        var plan = new AreaFlightPlan
        {
            InitialVelocity = _launchVelocity,
            StraightTime = _straightTime,
            FlightTime = _flightTime,
            Gravity = _planGravity,
            DropBlend = _dropBlend
        };
        Vector3 position = EvaluateAreaPosition(_launchPosition, plan, _aliveTimer);
        if (!TryMove(position - transform.position)) return;
        Vector3 tangent = EvaluateAreaVelocity(plan, _aliveTimer);
        if (tangent.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(tangent);
        if (_aliveTimer >= _flightTime) Explode(_impactPoint);
    }

    private void Explode(Vector3 position)
    {
        if (_hasHit) return;
        _hasHit = true;
        transform.position = position;
        ApplyAreaDamage(position);
        Destroy(gameObject);
    }

    private void ApplyAreaDamage(Vector3 position)
    {
        var damaged = new HashSet<object>();
        foreach (Collider hit in Physics.OverlapSphere(position, _areaRadius, ~0, QueryTriggerInteraction.Collide))
        {
            if (IsOwnedBySource(hit) || hit.GetComponentInParent<Enemy>() != null) continue;
            Player player = hit.GetComponentInParent<Player>();
            if (player != null)
            {
                if (damaged.Add(player)) player.TryDamage(_areaDamage);
                continue;
            }
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damaged.Add(damageable)) damageable.TakeDamage(_areaDamage);
        }
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

    private bool TryPlanAreaFlight(Vector3 start, Vector3 end, out AreaFlightPlan plan)
    {
        return TryPlanAreaFlight(start, end, _bulletSpeed, _straightDistance, _gravity, _dropBlendTime, out plan);
    }

    /// <summary>
    /// Flies straight on a constant velocity, then eases in gravity so the shell still lands on the target.
    /// </summary>
    public static bool TryPlanAreaFlight(
        Vector3 start,
        Vector3 end,
        float speed,
        float straightDistance,
        float gravity,
        float dropBlend,
        out AreaFlightPlan plan)
    {
        plan = default;
        float shotSpeed = Mathf.Max(0.01f, speed);
        Vector3 offset = end - start;
        Vector3 flat = Vector3.ProjectOnPlane(offset, Vector3.up);
        float xz = flat.magnitude;

        if (xz <= 0.001f)
        {
            float distance = offset.magnitude;
            if (distance <= 0.001f)
            {
                return false;
            }

            float verticalTime = Mathf.Max(0.05f, distance / shotSpeed);
            plan.InitialVelocity = offset / verticalTime;
            plan.StraightTime = verticalTime;
            plan.FlightTime = verticalTime;
            return true;
        }

        float flightTime = xz / shotSpeed;
        float straight = Mathf.Clamp(straightDistance, 0.0f, xz);
        float straightTime = straight / shotSpeed;
        float fallTime = flightTime - straightTime;
        float g = Mathf.Max(0.0f, gravity);
        float blend = Mathf.Max(0.0f, dropBlend);
        float drop = DropDisplacement(fallTime, g, blend);
        float verticalSpeed = (offset.y + drop) / flightTime;

        plan.InitialVelocity = flat / xz * shotSpeed + Vector3.up * verticalSpeed;
        plan.StraightTime = straightTime;
        plan.FlightTime = flightTime;
        plan.Gravity = g;
        plan.DropBlend = blend;
        return plan.InitialVelocity.sqrMagnitude > 0.0001f;
    }

    public static Vector3 EvaluateAreaPosition(Vector3 start, AreaFlightPlan plan, float time)
    {
        float t = Mathf.Clamp(time, 0.0f, Mathf.Max(0.0f, plan.FlightTime));
        float fall = Mathf.Max(0.0f, t - plan.StraightTime);
        return start + plan.InitialVelocity * t + Vector3.down * DropDisplacement(fall, plan.Gravity, plan.DropBlend);
    }

    public static Vector3 EvaluateAreaVelocity(AreaFlightPlan plan, float time)
    {
        float t = Mathf.Clamp(time, 0.0f, Mathf.Max(0.0f, plan.FlightTime));
        float fall = Mathf.Max(0.0f, t - plan.StraightTime);
        return plan.InitialVelocity + Vector3.down * DropSpeed(fall, plan.Gravity, plan.DropBlend);
    }

    private static float DropDisplacement(float fallTime, float gravity, float blend)
    {
        float t = Mathf.Max(0.0f, fallTime);
        float g = Mathf.Max(0.0f, gravity);
        if (t <= 0.0f || g <= 0.0f)
        {
            return 0.0f;
        }

        float b = Mathf.Max(0.0f, blend);
        if (b <= 0.0001f)
        {
            return 0.5f * g * t * t;
        }

        if (t < b)
        {
            float u = t / b;
            float u2 = u * u;
            float u4 = u2 * u2;
            return g * b * b * (0.25f * u4 - 0.1f * u4 * u);
        }

        float w = t - b;
        return g * (0.15f * b * b + 0.5f * b * w + 0.5f * w * w);
    }

    private static float DropSpeed(float fallTime, float gravity, float blend)
    {
        float t = Mathf.Max(0.0f, fallTime);
        float g = Mathf.Max(0.0f, gravity);
        if (t <= 0.0f || g <= 0.0f)
        {
            return 0.0f;
        }

        float b = Mathf.Max(0.0f, blend);
        if (b <= 0.0001f)
        {
            return g * t;
        }

        if (t < b)
        {
            float u = t / b;
            float u2 = u * u;
            return g * b * (u2 * u - 0.5f * u2 * u2);
        }

        return g * (0.5f * b + (t - b));
    }

    public struct AreaFlightPlan
    {
        public Vector3 InitialVelocity;
        public float StraightTime;
        public float FlightTime;
        public float Gravity;
        public float DropBlend;
    }
}
