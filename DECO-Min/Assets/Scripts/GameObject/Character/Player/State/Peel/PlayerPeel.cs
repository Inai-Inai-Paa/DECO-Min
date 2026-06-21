using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Player/Peel")]
public class PlayerPeel : PlayerState
{
    [Header("TransitionState")]
    [Tooltip("MoveState"),SerializeField] private PlayerState _moveState;

    [Header("Options")]
    [Tooltip("Peel行動成功したときの硬直時間"), SerializeField] private float _successDuration = 0.3f;
    [Tooltip("Peel行動失敗したときの硬直時間"), SerializeField] private float _failDuration = 0.2f;
    [Tooltip("Peel先オブジェクトのタグ"), SerializeField] private string _peelableTag = "Peelable";
    [Tooltip("Peel先オブジェクトのレイヤー"), SerializeField] private LayerMask _peelableLayer;
    [Tooltip("Peel行動の探知範囲"), SerializeField] private float _peelableRange = 0.5f;


    Collider[] _collider;
    private float _enterTime = 0.0f;
    GameObject _nearestPeelable = null;

    public override void Enter()
    {
        _collider = Physics.OverlapSphere(player.transform.position, _peelableRange, _peelableLayer);
        _nearestPeelable = null;
        foreach (var collider in _collider)
        {
            if (collider.CompareTag(_peelableTag))
            {
                //最も近い対象を取得する
                var currentDistance = _nearestPeelable != null ? Vector3.Distance(player.transform.position, _nearestPeelable.transform.position) : float.MaxValue;
                var newDistance = Vector3.Distance(player.transform.position, collider.transform.position);
                if (_nearestPeelable == null || newDistance < currentDistance)
                {
                    _nearestPeelable = collider.gameObject;
                }
            }
        }

        _enterTime = Time.time;
        Debug.Log("Peel"); 

    }

    public override void FixedUpdate()
    {
        if (_nearestPeelable)
        {
            //プレイヤー移動入力を取得したら_moveStateに遷移する、成功時硬直も同時に満たしていることを確認する
            if (player.playerInputData.Move.sqrMagnitude > 0.0f && Time.time - _enterTime >= _successDuration)
            {
                player.ChangePlayerState(_moveState);
            }
        }
        else
        {
            if (Time.time - _enterTime >= _failDuration)
            {
                player.ChangePlayerState(_moveState);
            }
        }
    }

    public override void Update()
    {

    }

    public override void Exit()
    {
        _enterTime = 0.0f;
    }
}
