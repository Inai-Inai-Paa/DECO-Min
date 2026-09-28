using System.Collections.Generic;
using UnityEngine;
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

        if (_itemPrefab == null)
        {
            Debug.LogError($"ItemPrefab is not assigned on {name}.", this);
            return false;
        }

        Instantiate(_itemPrefab, transform.position, transform.rotation);
        Die();
        return true;
    }

    protected void ValidateItemPrefab()
    {
        if (_itemPrefab != null)
        {
            return;
        }

        Debug.LogError($"ItemPrefab is not assigned on {name}.", this);
    }

    private void OnValidate()
    {
        ValidateItemPrefab();
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
