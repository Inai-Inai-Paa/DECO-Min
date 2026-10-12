using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public partial class Player : Character
{
    public PlayerInputData playerInputData;

    private PlayerStatus _status;
    public PlayerStatus Status => _status;
    [SerializeField] private PlayerSkill _skill;
    public PlayerSkill Skill => _skill;

    [Header("?X?e?[?g")]
    [Space(2)]
    [SerializeField]
    private PlayerState initState = null;
    [SerializeField] private PlayerDamage playerDamage = null; //???}???u ???????K?v????

    [Header("??n????")]
    [Space(2)]
    [SerializeField]
    private Vector3 groundCheckPos;     // ??????z?u?I?u?W?F?N?g
    [SerializeField]
    private LayerMask groundLayer;      // Ground???C???[???w??
    private float rayDistance = 0.2f;   // ??????L???????
    [HideInInspector]
    public bool isGrounded;

    private Camera mainCamera = null;
    [HideInInspector]
    public Vector3 cameraForward = Vector3.zero;

    private bool _isInvincible;
    private float _invincibleTimer;
    private float _knockbackTimer;
    private Vector3 _knockbackVelocity;
    private Rigidbody _knockbackBody;
    public float pendingDamage { get; private set; }

    // PeelAction
    private float _peelLastTime = -Mathf.Infinity;

    [Header("?A?j???[?V????")]
    [SerializeField] public Animator _animator;
    [SerializeField] private float _idleStartTime = 3f;
    private float _idleTimer;

    private void Awake()
    {
        //?v???C???[?X?e?[?^?X???L?????N?^?[?X?e?[?^?X??????
        _status = Instantiate((PlayerStatus)characterStatus);
        _status.TotalSealCount = 100;
        _status.CurrentSealCount = 100;
    }

    protected override void Start()
    {
        // Call the base class's Awake method to ensure that the state machine is initialized
        base.Start();

        InitializeInput();

        mainCamera = Camera.main;

        if (initState != null)
        {
            ChangePlayerState(Instantiate(initState));
        }
    }
    private void OnDestroy()
    {
        stateMachine?.Shutdown();
        FinalizeInput();
    }

    protected override void Update()
    {
        if (_isInvincible)
        {
            _invincibleTimer -= Time.deltaTime;

            if (_invincibleTimer <= 0.0f)
                _isInvincible = false;
        }

        base.Update();
    }

    protected override void FixedUpdate()
    {
        // ??????????????????????L???
        isGrounded = Physics.Raycast(transform.position + groundCheckPos, Vector3.down, rayDistance, groundLayer);
        cameraForward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized;

        //?A?j???[?V??????W
        _animator.SetBool("IsGround", isGrounded);

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        //?A?j???[?V?????????Q?????I???????t?O??A?j???[?V?????N??
        if ((stateInfo.IsName("Idle")))
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleStartTime)
            {
                _animator.SetTrigger("Idle");
                _idleTimer = 0.0f;
            }
        }
        else
        {
            _idleTimer = 0.0f;
        }

        base.FixedUpdate();
        TickKnockback();
    }

    public void ChangePlayerState(PlayerState nextState)
    {
        nextState.Initialize(this, stateMachine);
        stateMachine.ChangeState(nextState);
    }

    // ?_???[?W????
    public void ApplyDamage(float damage)
    {
        _status.currentHealth -= damage;

        if (_status.currentHealth <= 0.0f)
        {
            // ???S????
        }
    }

    public void StartInvincible(float time)
    {
        _isInvincible = true;
        _invincibleTimer = time;
    }

    public bool TryDamage(float damage)
    {
        if (_isInvincible)
            return false;

        pendingDamage = damage;
        ChangePlayerState(Instantiate(playerDamage));
        return true;
    }

    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        if (duration <= 0.01f || distance <= 0.0f)
        {
            return;
        }

        Vector3 flatDirection = direction;
        flatDirection.y = 0.0f;

        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        _knockbackVelocity = flatDirection.normalized * (distance / duration);
        _knockbackTimer = duration;
    }

    private void TickKnockback()
    {
        if (_knockbackTimer <= 0.0f)
        {
            return;
        }

        if (_knockbackBody == null)
        {
            _knockbackBody = GetComponent<Rigidbody>();
        }

        if (_knockbackBody != null)
        {
            Vector3 velocity = _knockbackBody.linearVelocity;
            velocity.x = _knockbackVelocity.x;
            velocity.z = _knockbackVelocity.z;
            _knockbackBody.linearVelocity = velocity;
        }

        _knockbackTimer -= Time.fixedDeltaTime;
    }

    // ?V?[??????????
    public void AddSeal(int amount, bool isAddTortal)
    {
        if (isAddTortal)
            _status.TotalSealCount += amount;
        _status.CurrentSealCount += amount;
    }

    public void RemoveSeal(int amount)
    {
        _status.CurrentSealCount -= amount;
        if (_status.CurrentSealCount < 0)
            _status.CurrentSealCount = 0;
    }

    // ?????s???N?[???_?E??
    public bool TryPeelAction(float cooldown)
    {
        if (Time.time - _peelLastTime >= cooldown)
        {
            _peelLastTime = Time.time;
            return true;
        }
        return false;
    }

    public void ResetPeelCooldown()
    {
        _peelLastTime = -Mathf.Infinity;
    }
}