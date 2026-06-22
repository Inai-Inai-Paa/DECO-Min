using Unity.VisualScripting;
using UnityEngine;

public partial class Player : Character
{
    public PlayerInputData playerInputData;

    [Header("Status")]
    private PlayerStatus _status;
    public PlayerStatus Status => _status;

    [Header("ステート")]
    [Space(2)]
    [SerializeField]
    private PlayerState initState = null;

    [Header("接地判定")]
    [Space(2)]
    [SerializeField]
    private Vector3 groundCheckPos;     // 足元の配置オブジェクト
    [SerializeField]
    private LayerMask groundLayer;      // Groundレイヤーを指定
    private float rayDistance = 0.2f;   // 光線を伸ばす長さ
    [HideInInspector]
    public bool isGrounded;

    private Camera mainCamera = null;
    [HideInInspector]
    public Vector3 cameraForward = Vector3.zero;

    private bool _isInvincible;
    private float _invincibleTimer;
    public float pendingDamage { get; private set; }

    protected override void Start()
    {
        // Call the base class's Awake method to ensure that the state machine is initialized
        base.Start();

        InitializeInput();

        mainCamera = Camera.main;

        //プレイヤーステータスをキャラクターステータスから取得
        _status = (PlayerStatus)characterStatus;

        if (initState != null)
        {
            ChangePlayerState(initState);
        }
    }
    private void OnDestroy()
    {
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
        // 足元から下に向かって光線を伸ばす
        isGrounded = Physics.Raycast(transform.position + groundCheckPos, Vector3.down, rayDistance, groundLayer);
        cameraForward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized;

        base.FixedUpdate();
    }

    public void ChangePlayerState(PlayerState nextState)
    {
        nextState.Initialize(this, stateMachine);
        stateMachine.ChangeState(nextState);
    }

    // ダメージ処理
    public void ApplyDamage(float damage)
    {
        characterStatus.currentHealth -= damage;

        if (characterStatus.currentHealth <= 0.0f)
        {
            // 死亡処理
        }
    }

    public void StartInvincible(float time)
    {
        _isInvincible = true;
        _invincibleTimer = time;
    }

    public void TryDamage(float damage, PlayerState damageState)
    {
        if (_isInvincible)
            return;

        pendingDamage = damage;
        ChangePlayerState(damageState);
    }

    // シール増減処理
    public void AddSeal(int amount)
    {
        _status.CurrentSealCount += amount;
    }

    public void RemoveSeal(int amount) {
        _status.CurrentSealCount -= amount;
        if (_status.CurrentSealCount < 0)
            _status.CurrentSealCount = 0;
    }
}
