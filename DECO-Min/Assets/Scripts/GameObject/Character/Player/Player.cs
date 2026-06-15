using UnityEngine;

public partial class Player : Character
{
    public PlayerInputData playerInputData;
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

    protected override void Start()
    {
        // Call the base class's Awake method to ensure that the state machine is initialized
        base.Start();

        InitializeInput();

        mainCamera = Camera.main;

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
};
