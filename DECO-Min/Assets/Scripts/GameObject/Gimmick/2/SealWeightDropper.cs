using System.Collections.Generic;
using UnityEngine;

public class SealWeightDropper : Gimmick
{
    [Header("シール設定")]
    [SerializeField] private int _requiredSealCount = 5;
    [SerializeField] private Transform _sealParent;

    [Header("移動設定")]
    [SerializeField] private Vector3 _activeOffset = new Vector3(0.0f, -3.0f, 0.0f);
    [SerializeField] private float _moveSpeed = 2.0f;

    [Header("ステート")]
    [SerializeField] private SealWeightDropActiveState _activeStateAsset;

    // 現在付着しているシール
    private readonly HashSet<DroppingSeal> _attachedSeals = new HashSet<DroppingSeal>();

    private Vector3 _defaultPosition;
    private Vector3 _activePosition;

    private bool _isStateStarted;

    public int CurrentSealCount => _attachedSeals.Count;
    public bool HasRequiredSeal => CurrentSealCount >= _requiredSealCount;

    protected override void Start()
    {
        base.Start();

        // 初期位置と作動後の位置を保存
        _defaultPosition = transform.position;
        _activePosition = _defaultPosition + _activeOffset;

        _activeState = _activeStateAsset;
    }

    protected override void FixedUpdate()
    {
        // 剥がされて破棄されたシールを除外
        RemoveDestroyedSeals();

        // 必要数に達したら作動ステートを開始
        if (!_isStateStarted && HasRequiredSeal)
            StartActiveState();

        base.FixedUpdate();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryAttachSeal(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAttachSeal(other);
    }

    private void TryAttachSeal(Collider hitCollider)
    {
        DroppingSeal seal = hitCollider.GetComponentInParent<DroppingSeal>();

        if (seal == null)
            return;

        // プレイヤーが生成したシールだけを対象にする
        if (seal.GetCreateSource() != SealCreateSource.Player)
            return;

        // 同じシールを重複して数えない
        if (!_attachedSeals.Add(seal))
            return;

        AttachSeal(seal);
    }

    private void AttachSeal(DroppingSeal seal)
    {
        Rigidbody sealRigidbody = seal.GetComponent<Rigidbody>();

        // シールを停止させてオブジェクトに固定
        if (sealRigidbody != null)
        {
            sealRigidbody.linearVelocity = Vector3.zero;
            sealRigidbody.angularVelocity = Vector3.zero;
            sealRigidbody.useGravity = false;
            sealRigidbody.isKinematic = true;
        }

        Transform parent = _sealParent != null ? _sealParent : transform;
        seal.transform.SetParent(parent, true);
    }

    private void RemoveDestroyedSeals()
    {
        _attachedSeals.RemoveWhere(seal => seal == null);
    }

    private void StartActiveState()
    {
        if (_activeState == null)
            return;

        _isStateStarted = true;

        // Assetを複製してから初期化
        GimmickActiveState activeState = Instantiate(_activeState);
        activeState.Initialize(this, _stateMachine);

        _stateMachine.ChangeState(activeState);
    }

    public void UpdateMovement()
    {
        Vector3 targetPosition = HasRequiredSeal ? _activePosition : _defaultPosition;

        // シール数に応じた位置へ移動
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            _moveSpeed * Time.fixedDeltaTime
        );
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 defaultPosition = Application.isPlaying ? _defaultPosition : transform.position;
        Vector3 activePosition = defaultPosition + _activeOffset;

        // 作動後の位置を表示
        Gizmos.DrawLine(defaultPosition, activePosition);
        Gizmos.DrawWireCube(activePosition, transform.localScale);
    }
}