using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyGroupFormation
{
    Center,
    Line,
    Triangle,
    Square,
    Pentagon,
    RandomInArea
}

/// <summary>
/// Spawns a fixed group when the player enters, and tracks defeat cooldowns separately from despawn.
/// </summary>
[DisallowMultipleComponent]
public class EnemyPopulationArea : MonoBehaviour
{
    private sealed class SpawnSlot
    {
        public Enemy Enemy;
        public float ReadyTime;
        public bool Defeated;
        public float OutsideTimer;
        public bool ChasingOutside;
        public bool HasEngaged;
        public bool Returning;
    }

    [Header("参照")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Transform _areaCenter;
    [SerializeField] private Collider _spawnVolume;
    [SerializeField] private Collider _searchVolume;

    [Header("出現")]
    [SerializeField, Range(1, 10)] private int _spawnCount = 3;
    [SerializeField] private EnemyGroupFormation _formation = EnemyGroupFormation.Line;
    [SerializeField, Min(0.5f)] private float _formationSpacing = 2.0f;
    [SerializeField] private Vector3 _randomAreaSize = new Vector3(8.0f, 0.0f, 8.0f);
    [SerializeField, Min(0.0f)] private float _minPlayerDistance = 2.0f;

    [Header("NavMesh")]
    [SerializeField] private bool _snapToNavMesh = true;
    [SerializeField, Min(0.1f)] private float _navMeshSearchDistance = 3.0f;

    private readonly List<SpawnSlot> _slots = new List<SpawnSlot>();
    private float _playerOutsideTimer;
    private bool _spawnLatched;

    public int SpawnCount => _spawnCount;
    public int AliveCount => CountAlive();
    private Transform CenterTransform => _areaCenter != null ? _areaCenter : transform;

    private void Awake()
    {
        if (_areaCenter == null)
        {
            _areaCenter = transform;
        }

        if (_spawnVolume == null)
        {
            _spawnVolume = GetComponent<Collider>();
        }

        EnsureSlotCount();
    }

    private void OnEnable()
    {
        EnemyManager2.Instance?.RegisterArea(this);
    }

    private void Start()
    {
        EnemyManager2.Instance?.RegisterArea(this);
    }

    private void OnDisable()
    {
        EnemyManager2.Instance?.UnregisterArea(this);
    }

    private void Update()
    {
        EnsureSlotCount();
        TickPlayerAbsence(Time.deltaTime);
        TickChaseOutside(Time.deltaTime);
        TickSpawnEntry();
    }

    public void ResetForRestart()
    {
        DespawnAlive();

        for (int i = 0; i < _slots.Count; i++)
        {
            SpawnSlot slot = _slots[i];
            slot.Defeated = false;
            slot.ReadyTime = 0.0f;
            slot.OutsideTimer = 0.0f;
            slot.ChasingOutside = false;
            slot.HasEngaged = false;
            slot.Returning = false;
        }

        _playerOutsideTimer = 0.0f;
        _spawnLatched = IsPlayerInside(_spawnVolume);
    }

    private void TickSpawnEntry()
    {
        bool insideSpawn = IsPlayerInside(_spawnVolume);

        if (!insideSpawn)
        {
            _spawnLatched = false;
            return;
        }

        if (_spawnLatched)
        {
            return;
        }

        _spawnLatched = true;
        TrySpawn();
    }

    private void TickPlayerAbsence(float deltaTime)
    {
        if (IsPlayerInsideSearch())
        {
            _playerOutsideTimer = 0.0f;
            return;
        }

        if (CountAlive() == 0)
        {
            _playerOutsideTimer = 0.0f;
            return;
        }

        _playerOutsideTimer += deltaTime;

        if (_playerOutsideTimer < DespawnDelaySeconds())
        {
            return;
        }

        _playerOutsideTimer = 0.0f;
        DespawnAlive();
        EnemyManager2.Instance?.NotifyGroupDespawned();
    }

    private void TickChaseOutside(float deltaTime)
    {
        float limit = ChaseOutsideSeconds();

        for (int i = 0; i < _slots.Count; i++)
        {
            SpawnSlot slot = _slots[i];

            if (!IsSlotAlive(slot) || slot.Returning)
            {
                continue;
            }

            if (slot.Enemy.Awareness != EnemyAwareness.Unaware)
            {
                slot.HasEngaged = true;
            }

            bool outside = !IsInside(_searchVolume != null ? _searchVolume : _spawnVolume, slot.Enemy.transform.position);

            if (!outside)
            {
                slot.OutsideTimer = 0.0f;
                slot.ChasingOutside = false;
                continue;
            }

            if (slot.HasEngaged)
            {
                slot.ChasingOutside = true;
            }

            if (!slot.ChasingOutside)
            {
                continue;
            }

            slot.OutsideTimer += deltaTime;

            if (slot.OutsideTimer < limit)
            {
                continue;
            }

            slot.Returning = true;
            slot.OutsideTimer = 0.0f;
            slot.Enemy.BeginPopulationReturn();
        }
    }

    private void TrySpawn()
    {
        if (_enemyPrefab == null || CountAlive() > 0)
        {
            return;
        }

        List<SpawnSlot> eligible = new List<SpawnSlot>();

        for (int i = 0; i < _slots.Count; i++)
        {
            if (IsSlotSpawnable(_slots[i]))
            {
                eligible.Add(_slots[i]);
            }
        }

        if (eligible.Count == 0)
        {
            return;
        }

        List<Vector3> positions = BuildFormation(eligible.Count);
        int spawned = 0;

        for (int i = 0; i < eligible.Count; i++)
        {
            Vector3 position = i < positions.Count ? positions[i] : CenterTransform.position;
            position = KeepAwayFromPlayer(position);
            position = Snap(position);

            if (!TrySpawnSlot(eligible[i], position))
            {
                continue;
            }

            spawned++;
        }

        if (spawned > 0)
        {
            EnemyManager2.Instance?.NotifyGroupSpawned();
        }
    }

    private bool TrySpawnSlot(SpawnSlot slot, Vector3 position)
    {
        Quaternion rotation = LookRotation(position);
        GameObject enemyObject = Instantiate(_enemyPrefab, position, rotation);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy == null)
        {
            Destroy(enemyObject);
            return false;
        }

        enemy.SetSpawnPose(position, rotation);
        enemy.Removed += HandleEnemyRemoved;
        slot.Enemy = enemy;
        slot.Defeated = false;
        slot.ReadyTime = 0.0f;
        slot.OutsideTimer = 0.0f;
        slot.ChasingOutside = false;
        slot.HasEngaged = false;
        slot.Returning = false;
        return true;
    }

    private void HandleEnemyRemoved(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.Removed -= HandleEnemyRemoved;
        SpawnSlot slot = FindSlot(enemy);

        if (slot == null)
        {
            return;
        }

        bool defeated = enemy.RemovalReason == EnemyRemovalReason.Defeated;
        slot.Enemy = null;
        slot.Returning = false;
        slot.OutsideTimer = 0.0f;
        slot.ChasingOutside = false;
        slot.HasEngaged = false;

        if (!defeated)
        {
            slot.Defeated = false;
            slot.ReadyTime = 0.0f;
            return;
        }

        slot.Defeated = true;
        EnemyManager2.Instance?.NotifyEnemyDefeated(enemy);

        if (CountAlive() > 0 || HasDespawnedSlot())
        {
            slot.ReadyTime = Time.time + DespawnDelaySeconds();
            return;
        }

        float wipeTime = Time.time + WipeRespawnSeconds();

        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Defeated = true;
            _slots[i].ReadyTime = wipeTime;
        }

        EnemyManager2.Instance?.NotifyGroupWiped();
    }

    private void DespawnAlive()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            SpawnSlot slot = _slots[i];

            if (!IsSlotAlive(slot))
            {
                continue;
            }

            Enemy enemy = slot.Enemy;
            enemy.Removed -= HandleEnemyRemoved;
            slot.Enemy = null;
            slot.Defeated = false;
            slot.ReadyTime = 0.0f;
            slot.Returning = false;
            slot.OutsideTimer = 0.0f;
            slot.ChasingOutside = false;
            slot.HasEngaged = false;
            enemy.Despawn();
        }
    }

    private SpawnSlot FindSlot(Enemy enemy)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Enemy == enemy)
            {
                return _slots[i];
            }
        }

        return null;
    }

    private bool IsSlotSpawnable(SpawnSlot slot)
    {
        if (slot.Enemy != null)
        {
            return false;
        }

        return !slot.Defeated || Time.time >= slot.ReadyTime;
    }

    private bool IsSlotAlive(SpawnSlot slot)
    {
        return slot.Enemy != null && !slot.Enemy.IsDead && !slot.Enemy.IsDespawning;
    }

    private int CountAlive()
    {
        int count = 0;

        for (int i = 0; i < _slots.Count; i++)
        {
            if (IsSlotAlive(_slots[i]))
            {
                count++;
            }
        }

        return count;
    }

    private bool HasDespawnedSlot()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            SpawnSlot slot = _slots[i];

            if (slot.Enemy == null && !slot.Defeated)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureSlotCount()
    {
        int count = Mathf.Clamp(_spawnCount, 1, 10);
        _spawnCount = count;

        while (_slots.Count < count)
        {
            _slots.Add(new SpawnSlot());
        }

        while (_slots.Count > count)
        {
            int last = _slots.Count - 1;
            SpawnSlot slot = _slots[last];

            if (IsSlotAlive(slot))
            {
                Enemy enemy = slot.Enemy;
                enemy.Removed -= HandleEnemyRemoved;
                slot.Enemy = null;
                enemy.Despawn();
            }

            _slots.RemoveAt(last);
        }
    }

    private List<Vector3> BuildFormation(int count)
    {
        List<Vector3> positions = new List<Vector3>(count);
        Vector3 center = CenterTransform.position;
        Quaternion rotation = AreaRotation();

        if (count <= 1 || _formation == EnemyGroupFormation.Center)
        {
            AppendCenter(positions, count);
        }
        else if (_formation == EnemyGroupFormation.Line)
        {
            AppendLine(positions, count);
        }
        else if (_formation == EnemyGroupFormation.RandomInArea)
        {
            AppendRandom(positions, count);
            return positions;
        }
        else
        {
            int sides = SidesFor(_formation);
            Vector3[] offsets = BuildPolygonOffsets(sides, count, _formationSpacing);

            for (int i = 0; i < offsets.Length; i++)
            {
                positions.Add(center + rotation * offsets[i]);
            }

            return positions;
        }

        for (int i = 0; i < positions.Count; i++)
        {
            positions[i] = center + rotation * positions[i];
        }

        return positions;
    }

    private void AppendCenter(List<Vector3> positions, int count)
    {
        if (count <= 1)
        {
            positions.Add(Vector3.zero);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float angle = (360.0f / count) * i * Mathf.Deg2Rad;
            positions.Add(new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * _formationSpacing);
        }
    }

    private void AppendLine(List<Vector3> positions, int count)
    {
        float origin = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            positions.Add(Vector3.right * ((i - origin) * _formationSpacing));
        }
    }

    private void AppendRandom(List<Vector3> positions, int count)
    {
        Vector3 center = CenterTransform.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 point = center;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                float x = Random.Range(-_randomAreaSize.x * 0.5f, _randomAreaSize.x * 0.5f);
                float z = Random.Range(-_randomAreaSize.z * 0.5f, _randomAreaSize.z * 0.5f);
                point = center + AreaRotation() * new Vector3(x, 0.0f, z);

                if (!IsCrowded(point, positions))
                {
                    break;
                }
            }

            positions.Add(point);
        }
    }

    private static bool IsCrowded(Vector3 point, List<Vector3> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 offset = point - positions[i];
            offset.y = 0.0f;

            if (offset.sqrMagnitude < 1.0f)
            {
                return true;
            }
        }

        return false;
    }

    private static int SidesFor(EnemyGroupFormation formation)
    {
        switch (formation)
        {
            case EnemyGroupFormation.Square:
                return 4;
            case EnemyGroupFormation.Pentagon:
                return 5;
            default:
                return 3;
        }
    }

    private static Vector3[] BuildPolygonOffsets(int sides, int count, float radius)
    {
        Vector3[] vertices = new Vector3[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = (90.0f - (360.0f / sides) * i) * Mathf.Deg2Rad;
            vertices[i] = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * radius;
        }

        float edgeLength = Vector3.Distance(vertices[0], vertices[1]);
        float perimeter = edgeLength * sides;
        Vector3[] points = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            points[i] = PointAlongPolygon(vertices, perimeter * i / count, edgeLength);
        }

        return points;
    }

    private static Vector3 PointAlongPolygon(Vector3[] vertices, float distance, float edgeLength)
    {
        float remaining = distance;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (remaining <= edgeLength || i == vertices.Length - 1)
            {
                float t = edgeLength <= 0.0f ? 0.0f : Mathf.Clamp01(remaining / edgeLength);
                return Vector3.Lerp(vertices[i], vertices[(i + 1) % vertices.Length], t);
            }

            remaining -= edgeLength;
        }

        return vertices[0];
    }

    private Vector3 KeepAwayFromPlayer(Vector3 position)
    {
        Transform player = PlayerTransform();

        if (player == null || _minPlayerDistance <= 0.0f)
        {
            return position;
        }

        Vector3 offset = position - player.position;
        offset.y = 0.0f;
        float distance = offset.magnitude;

        if (distance >= _minPlayerDistance)
        {
            return position;
        }

        Vector3 direction = distance > 0.01f ? offset / distance : CenterTransform.right;
        Vector3 pushed = player.position + direction * _minPlayerDistance;
        pushed.y = position.y;
        return pushed;
    }

    private Vector3 Snap(Vector3 position)
    {
        if (!_snapToNavMesh)
        {
            return position;
        }

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return position;
    }

    private Quaternion LookRotation(Vector3 position)
    {
        Vector3 direction = position - CenterTransform.position;
        direction.y = 0.0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = AreaRotation() * Vector3.forward;
        }

        if (direction.sqrMagnitude < 0.01f)
        {
            return CenterTransform.rotation;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Quaternion AreaRotation()
    {
        Vector3 forward = CenterTransform.forward;
        forward.y = 0.0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private bool IsPlayerInsideSearch()
    {
        if (IsPlayerInside(_spawnVolume))
        {
            return true;
        }

        return IsPlayerInside(_searchVolume != null ? _searchVolume : _spawnVolume);
    }

    private bool IsPlayerInside(Collider volume)
    {
        Transform player = PlayerTransform();

        if (player == null)
        {
            return false;
        }

        return IsInside(volume, player.position);
    }

    private static bool IsInside(Collider volume, Vector3 worldPoint)
    {
        if (volume == null || !volume.enabled)
        {
            return false;
        }

        if (volume is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float scale = MaxAbsScale(sphere.transform.lossyScale);
            float radius = sphere.radius * scale;
            return (worldPoint - center).sqrMagnitude <= radius * radius;
        }

        if (volume is BoxCollider box)
        {
            Vector3 local = box.transform.InverseTransformPoint(worldPoint) - box.center;
            Vector3 half = box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x
                && Mathf.Abs(local.y) <= half.y
                && Mathf.Abs(local.z) <= half.z;
        }

        if (volume is CapsuleCollider capsule)
        {
            return IsInsideCapsule(capsule, worldPoint);
        }

        return volume.bounds.Contains(worldPoint);
    }

    private static bool IsInsideCapsule(CapsuleCollider capsule, Vector3 worldPoint)
    {
        Vector3 center = capsule.transform.TransformPoint(capsule.center);
        float scale = MaxAbsScale(capsule.transform.lossyScale);
        float radius = capsule.radius * scale;
        float height = Mathf.Max(capsule.height * scale, radius * 2.0f);
        float half = height * 0.5f - radius;
        Vector3 axis = Axis(capsule.transform, capsule.direction);
        Vector3 a = center + axis * half;
        Vector3 b = center - axis * half;
        Vector3 closest = ClosestPointOnSegment(a, b, worldPoint);
        return (worldPoint - closest).sqrMagnitude <= radius * radius;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float length = ab.sqrMagnitude;

        if (length <= 0.0001f)
        {
            return a;
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / length);
        return a + ab * t;
    }

    private static Vector3 Axis(Transform target, int direction)
    {
        switch (direction)
        {
            case 0:
                return target.right;
            case 2:
                return target.forward;
            default:
                return target.up;
        }
    }

    private static float MaxAbsScale(Vector3 scale)
    {
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }

    private Transform PlayerTransform()
    {
        if (EnemyManager2.Instance != null && EnemyManager2.Instance.PlayerTransform != null)
        {
            return EnemyManager2.Instance.PlayerTransform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    private float WipeRespawnSeconds()
    {
        return EnemyManager2.Instance != null ? EnemyManager2.Instance.GroupWipeRespawnSeconds : 300.0f;
    }

    private float DespawnDelaySeconds()
    {
        return EnemyManager2.Instance != null ? EnemyManager2.Instance.DespawnDelaySeconds : 10.0f;
    }

    private float ChaseOutsideSeconds()
    {
        return EnemyManager2.Instance != null ? EnemyManager2.Instance.ChaseOutsideSeconds : 10.0f;
    }

    private void OnValidate()
    {
        _spawnCount = Mathf.Clamp(_spawnCount, 1, 10);

        if (_spawnVolume != null)
        {
            _spawnVolume.isTrigger = true;
        }

        if (_searchVolume != null)
        {
            _searchVolume.isTrigger = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = _areaCenter != null ? _areaCenter : transform;
        List<Vector3> positions = BuildFormation(Mathf.Clamp(_spawnCount, 1, 10));
        Gizmos.color = Color.green;

        for (int i = 0; i < positions.Count; i++)
        {
            Gizmos.DrawWireSphere(positions[i], 0.35f);
        }

        if (_formation == EnemyGroupFormation.RandomInArea)
        {
            Gizmos.color = Color.cyan;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center.position, AreaRotation(), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_randomAreaSize.x, 0.05f, _randomAreaSize.z));
            Gizmos.matrix = previous;
        }

        DrawVolume(_spawnVolume, Color.yellow);
        DrawVolume(_searchVolume != null ? _searchVolume : _spawnVolume, new Color(1.0f, 0.4f, 0.2f, 1.0f));
    }

    private static void DrawVolume(Collider volume, Color color)
    {
        if (volume == null)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.matrix = volume.transform.localToWorldMatrix;

        if (volume is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else if (volume is BoxCollider box)
        {
            Gizmos.DrawWireCube(box.center, box.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
