using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public partial class Enemy
{
    public const float StunDuration = 15.0f;
    public const int FinisherBodyLengthCount = 4;

    private const float DisappearDuration = 1.0f;

    private struct ColliderPassState
    {
        public Collider Collider;
        public LayerMask ExcludeLayers;
        public bool WasTrigger;
    }

    private struct FadeMaterialState
    {
        public Material Material;
        public bool HasBaseColor;
        public Color BaseColor;
        public bool HasColor;
        public Color Color;
    }

    private readonly List<ColliderPassState> _passThroughColliders = new List<ColliderPassState>();
    private readonly List<FadeMaterialState> _fadeMaterials = new List<FadeMaterialState>();

    private bool _isStunned;
    private bool _isFinishing;
    private bool _isDisappearing;
    private float _stunTimer;
    private float _disappearTimer;
    private bool _hasStoredAgentState;
    private bool _storedAgentEnabled;
    private bool _hasStoredConstraints;
    private RigidbodyConstraints _storedConstraints;
    private Rigidbody _frozenBody;

    public bool IsStunned => _isStunned;
    public bool IsFinishing => _isFinishing;
    public bool CanBeFinished =>
        _isStunned && !_isFinishing && !_isDisappearing && !_isDead && !_isDespawning;
    public bool IsCombatAIFrozen => _isStunned || _isFinishing || _isDisappearing;

    public static float GetPlayerBodyLength(Transform player)
    {
        if (player == null)
        {
            return 1.0f;
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();

        if (capsule == null)
        {
            capsule = player.GetComponentInChildren<CapsuleCollider>();
        }

        if (capsule != null)
        {
            Vector3 scale = capsule.transform.lossyScale;
            float axisScale = capsule.direction switch
            {
                0 => Mathf.Abs(scale.x),
                2 => Mathf.Abs(scale.z),
                _ => Mathf.Abs(scale.y)
            };

            return Mathf.Max(0.01f, capsule.height * axisScale);
        }

        Collider bodyCollider = player.GetComponent<Collider>();

        if (bodyCollider == null)
        {
            bodyCollider = player.GetComponentInChildren<Collider>();
        }

        if (bodyCollider != null)
        {
            return Mathf.Max(0.01f, bodyCollider.bounds.size.y);
        }

        return 1.0f;
    }

    public static float GetFinisherRange(Transform player)
    {
        return GetPlayerBodyLength(player) * FinisherBodyLengthCount;
    }

    public static Enemy SelectFinisherTarget(Transform player)
    {
        if (player == null)
        {
            return null;
        }

        Vector3 gaze = GetPlayerGaze(player);
        Vector3 origin = player.position;
        float range = GetFinisherRange(player);
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Enemy bestTarget = null;
        float bestLateralSqr = float.MaxValue;
        float bestForward = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.CanBeFinished)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - origin;
            offset.y = 0.0f;
            float forward = Vector3.Dot(offset, gaze);

            if (forward <= 0.0f || offset.magnitude > range)
            {
                continue;
            }

            Vector3 closestOnGaze = gaze * forward;
            float lateralSqr = (offset - closestOnGaze).sqrMagnitude;

            if (lateralSqr < bestLateralSqr - 0.000001f
                || (Mathf.Abs(lateralSqr - bestLateralSqr) <= 0.000001f && forward < bestForward))
            {
                bestTarget = enemy;
                bestLateralSqr = lateralSqr;
                bestForward = forward;
            }
        }

        return bestTarget;
    }

    public bool CanBeFinishedBy(Transform player)
    {
        return player != null && SelectFinisherTarget(player) == this;
    }

    public bool TryBeginFinisher(Transform player)
    {
        if (!CanBeFinishedBy(player))
        {
            return false;
        }

        BeginFinisher();
        return true;
    }

    public bool CompleteFinisher()
    {
        if (!_isFinishing || _isDead || _isDespawning)
        {
            return false;
        }

        if (_sealPrefab == null)
        {
            Debug.LogError($"SealPrefab is not assigned on {name}.", this);
            return false;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;
        Vector3 surfaceNormal = Vector3.up;

        if (TryGetGroundSurface(out Vector3 groundPoint, out Vector3 groundNormal))
        {
            spawnPosition = groundPoint;
            surfaceNormal = groundNormal;
            spawnRotation = RotationOnSurface(groundNormal);
        }

        GameObject spawned = Instantiate(_sealPrefab, spawnPosition, spawnRotation);
        StickSealToSurface(spawned.transform, spawnPosition, surfaceNormal);
        DroppingSeal droppingSeal = spawned.GetComponent<DroppingSeal>();

        if (droppingSeal == null)
        {
            droppingSeal = spawned.GetComponentInChildren<DroppingSeal>();
        }

        if (droppingSeal != null)
        {
            droppingSeal.SetCreateSource(SealCreateSource.Enemy);
        }

        Die();
        return true;
    }

    private bool TryGetGroundSurface(out Vector3 groundPoint, out Vector3 groundNormal)
    {
        const float originHeight = 2.0f;
        const float castDistance = 12.0f;

        Vector3 origin = transform.position + Vector3.up * originHeight;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            originHeight + castDistance,
            ResolveGroundMask(),
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        bool found = false;
        groundPoint = transform.position;
        groundNormal = Vector3.up;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null || hitCollider.isTrigger || IsOwnCollider(hitCollider))
            {
                continue;
            }

            if (hits[i].distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hits[i].distance;
            groundPoint = hits[i].point;
            groundNormal = hits[i].normal.sqrMagnitude > 0.0001f
                ? hits[i].normal.normalized
                : Vector3.up;
            found = true;
        }

        if (found)
        {
            return true;
        }

        float searchDistance = Mathf.Max(_navMeshSearchDistance, 3.0f);

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, searchDistance, NavMesh.AllAreas))
        {
            return false;
        }

        groundPoint = new Vector3(transform.position.x, navHit.position.y, transform.position.z);
        groundNormal = Vector3.up;
        return true;
    }

    private LayerMask ResolveGroundMask()
    {
        if (_groundLayer.value != 0)
        {
            return _groundLayer;
        }

        return LayerMask.GetMask("Default", "Ground");
    }

    private bool IsOwnCollider(Collider hitCollider)
    {
        Transform hitTransform = hitCollider.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private Quaternion RotationOnSurface(Vector3 surfaceNormal)
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.right, surfaceNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return Quaternion.FromToRotation(Vector3.up, surfaceNormal);
        }

        return Quaternion.LookRotation(forward.normalized, surfaceNormal);
    }

    private static void StickSealToSurface(Transform seal, Vector3 surfacePoint, Vector3 surfaceNormal)
    {
        if (!TryGetLowestAlongNormal(seal, surfaceNormal, out float lowest))
        {
            return;
        }

        const float skin = 0.01f;
        float surfaceHeight = Vector3.Dot(surfacePoint, surfaceNormal);
        seal.position += surfaceNormal * (surfaceHeight + skin - lowest);
    }

    private static bool TryGetLowestAlongNormal(Transform root, Vector3 surfaceNormal, out float lowest)
    {
        lowest = float.PositiveInfinity;
        bool found = false;
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];

            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            if (!filter.TryGetComponent(out Renderer renderer) || !renderer.enabled)
            {
                continue;
            }

            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 worldCorner = filter.transform.TransformPoint(corner);
                        float height = Vector3.Dot(worldCorner, surfaceNormal);

                        if (height < lowest)
                        {
                            lowest = height;
                        }

                        found = true;
                    }
                }
            }
        }

        return found;
    }

    protected void ValidateSealPrefab()
    {
        if (_sealPrefab != null)
        {
            return;
        }

        Debug.LogError($"SealPrefab is not assigned on {name}.", this);
    }

    private void OnValidate()
    {
        ValidateSealPrefab();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        HandleIncomingAttack(collision.collider);
    }

    private void TickStunState()
    {
        if (!_isStunned || _isFinishing || _isDead || _isDespawning)
        {
            return;
        }

        if (_isDisappearing)
        {
            _disappearTimer += Time.deltaTime;
            ApplyDisappear(_disappearTimer / DisappearDuration);

            if (_disappearTimer >= DisappearDuration)
            {
                Despawn();
            }

            return;
        }

        _stunTimer += Time.deltaTime;

        if (_stunTimer >= StunDuration)
        {
            BeginDisappear();
        }
    }

    private void EnterStun()
    {
        if (_isDead || _isDespawning || _isStunned || _isFinishing)
        {
            return;
        }

        _animator.SetTrigger("Stun");
        _isStunned = true;
        _isDown = true;
        _stunTimer = 0.0f;
        _disappearTimer = 0.0f;
        _isDisappearing = false;
        SetCommittedAttack(false);
        ClearAwareness();

        if (stateMachine != null)
        {
            stateMachine.Shutdown();
        }

        FreezeMotion();
        EnablePassThrough();
    }

    private void BeginFinisher()
    {
        _isFinishing = true;
        SetCommittedAttack(false);
        ClearAwareness();

        if (stateMachine != null)
        {
            stateMachine.Shutdown();
        }

        FreezeMotion();
    }

    private void ClearStunState()
    {
        RestoreFade();
        RestorePassThrough();
        RestoreMotion();
        _isStunned = false;
        _isFinishing = false;
        _isDisappearing = false;
        _stunTimer = 0.0f;
        _disappearTimer = 0.0f;
    }

    private void FreezeMotion()
    {
        if (_agent != null)
        {
            if (!_hasStoredAgentState)
            {
                _storedAgentEnabled = _agent.enabled;
                _hasStoredAgentState = true;
            }

            if (_agent.enabled)
            {
                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _agent.velocity = Vector3.zero;
                    _agent.nextPosition = transform.position;
                }

                _agent.enabled = false;
            }
        }

        if (_frozenBody == null)
        {
            _frozenBody = GetComponent<Rigidbody>();
        }

        if (_frozenBody == null)
        {
            return;
        }

        if (!_hasStoredConstraints)
        {
            _storedConstraints = _frozenBody.constraints;
            _hasStoredConstraints = true;
        }

        _frozenBody.linearVelocity = Vector3.zero;
        _frozenBody.angularVelocity = Vector3.zero;
        _frozenBody.constraints = RigidbodyConstraints.FreezeAll;
    }

    private void RestoreMotion()
    {
        if (_agent != null && _hasStoredAgentState)
        {
            _agent.enabled = _storedAgentEnabled;
            _hasStoredAgentState = false;
        }

        if (_frozenBody != null && _hasStoredConstraints)
        {
            _frozenBody.constraints = _storedConstraints;
            _hasStoredConstraints = false;
        }
    }

    private void EnablePassThrough()
    {
        if (_passThroughColliders.Count > 0)
        {
            return;
        }

        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer < 0)
        {
            return;
        }

        int playerBit = 1 << playerLayer;
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            _passThroughColliders.Add(new ColliderPassState
            {
                Collider = collider,
                ExcludeLayers = collider.excludeLayers,
                WasTrigger = collider.isTrigger
            });

            LayerMask excludeLayers = collider.excludeLayers;
            excludeLayers.value |= playerBit;
            collider.excludeLayers = excludeLayers;
            collider.isTrigger = true;
        }
    }

    private void RestorePassThrough()
    {
        for (int i = 0; i < _passThroughColliders.Count; i++)
        {
            ColliderPassState state = _passThroughColliders[i];

            if (state.Collider != null)
            {
                state.Collider.excludeLayers = state.ExcludeLayers;
                state.Collider.isTrigger = state.WasTrigger;
            }
        }

        _passThroughColliders.Clear();
    }

    private bool IsPlayerAttack(Collider other)
    {
        if (other == null || IsFinisherAttack(other))
        {
            return false;
        }

        Transform current = other.transform;

        while (current != null)
        {
            if (!string.IsNullOrEmpty(_sealAttackTag) && current.gameObject.tag == _sealAttackTag)
            {
                return true;
            }

            if (current.GetComponent<Attack>() != null || current.GetComponent<Cracker>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Vector3 GetPlayerGaze(Transform player)
    {
        Vector3 gaze = player.forward;
        Player playerComponent = player.GetComponent<Player>();

        if (playerComponent == null)
        {
            playerComponent = player.GetComponentInParent<Player>();
        }

        if (playerComponent != null && playerComponent.cameraForward.sqrMagnitude > 0.0001f)
        {
            gaze = playerComponent.cameraForward;
        }

        gaze.y = 0.0f;

        if (gaze.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return gaze.normalized;
    }

    private void BeginDisappear()
    {
        _isDisappearing = true;
        _disappearTimer = 0.0f;
        _fadeMaterials.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.materials;

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                PrepareTransparent(material);
                _fadeMaterials.Add(new FadeMaterialState
                {
                    Material = material,
                    HasBaseColor = material.HasProperty("_BaseColor"),
                    BaseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white,
                    HasColor = material.HasProperty("_Color"),
                    Color = material.HasProperty("_Color") ? material.color : Color.white
                });
            }
        }
    }

    private void ApplyDisappear(float normalizedTime)
    {
        float alpha = 1.0f - Mathf.Clamp01(normalizedTime);

        for (int i = 0; i < _fadeMaterials.Count; i++)
        {
            FadeMaterialState state = _fadeMaterials[i];

            if (state.Material == null)
            {
                continue;
            }

            if (state.HasBaseColor)
            {
                Color color = state.BaseColor;
                color.a *= alpha;
                state.Material.SetColor("_BaseColor", color);
            }

            if (state.HasColor)
            {
                Color color = state.Color;
                color.a *= alpha;
                state.Material.SetColor("_Color", color);
            }
        }
    }

    private void RestoreFade()
    {
        for (int i = 0; i < _fadeMaterials.Count; i++)
        {
            FadeMaterialState state = _fadeMaterials[i];

            if (state.Material == null)
            {
                continue;
            }

            if (state.HasBaseColor)
            {
                state.Material.SetColor("_BaseColor", state.BaseColor);
            }

            if (state.HasColor)
            {
                state.Material.SetColor("_Color", state.Color);
            }
        }

        _fadeMaterials.Clear();
    }

    private static void PrepareTransparent(Material material)
    {
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1.0f);
            material.SetFloat("_Blend", 0.0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return;
        }

        if (!material.HasProperty("_Mode"))
        {
            return;
        }

        material.SetFloat("_Mode", 3.0f);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }
}
