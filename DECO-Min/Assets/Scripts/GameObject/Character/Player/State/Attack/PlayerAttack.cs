using UnityEngine;

[CreateAssetMenu(menuName = "State/Player/Attack")]
public class PlayerAttack : PlayerState
{
    [SerializeField] private GameObject _attackPrefab;

    [SerializeField] private PlayerMove _playerMove;

    private float _frameCounter; //カウンタ
    [SerializeField] private float _attackStartUpTime;//攻撃が発生前の時間
    [SerializeField] private float _attackRecoveryTime;//攻撃が終わった後の硬直時間

    private bool _wasAttack = false;//アタックをしたか

    public override void Enter()
    {
        _frameCounter = 0;//初期化
        _wasAttack = false;
    }

    /// <summary>
    /// カウンタを回して、行動を管理
    /// </summary>
    public override void FixedUpdate()
    {
        _frameCounter += Time.deltaTime;

        if(_wasAttack)
        {
            if (_frameCounter >= _attackRecoveryTime) player.ChangePlayerState(_playerMove);

            return;
        }

        if(_frameCounter >= _attackStartUpTime)
        {
            Attack();
            _frameCounter = 0;
        }
    }

    /// <summary>
    /// 攻撃の発生 ここではインスタンスだけで、挙動はクラッカーに任せる
    /// </summary>
    private void Attack()
    {
        Debug.Log("aa");
        Instantiate(_attackPrefab, player.transform.position, player.transform.rotation);
        _wasAttack = true;
    }
}
