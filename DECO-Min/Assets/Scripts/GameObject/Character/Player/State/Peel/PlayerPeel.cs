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
    public float GetPeelableRange() { return _peelableRange; }

    [Tooltip("Peel成功時にまとまって剥がす範囲"), SerializeField] private float _peelableChainRange = 2.0f;
    public float GetPeelableChainRange() { return _peelableChainRange; }

    [Tooltip("加算されるシールの数"), SerializeField] private int _addSealCount = 1;

    Collider[] _collider;
    private float _enterTime = 0.0f;
    DroppingSeal _nearestPeelable = null;

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
                    //対象がDroppingSealであることを再度確認する
                    if (collider.gameObject.GetComponent<DroppingSeal>() != null)
                    {
                        _nearestPeelable = collider.gameObject.GetComponent<DroppingSeal>();
                        PeelAction(); //最初の1回は剥がすアクションを自動で行う
                    }  
                }
            }
        }

        _enterTime = Time.time;

    }

    public override void FixedUpdate()
    {
        if (_nearestPeelable)
        {
            if (player.playerInputData.PeelScrollPressed)
            {
                PeelAction();
            }
            //プレイヤー移動入力を取得したら_moveStateに遷移する、成功時硬直も同時に満たしていることを確認する
            if (player.playerInputData.Move.sqrMagnitude > 0.0f && Time.time - _enterTime >= _successDuration)
            {
                player.ChangePlayerState(Instantiate(_moveState));
            }
        }
        else
        {
            if (Time.time - _enterTime >= _failDuration)
            {
                player.ChangePlayerState(Instantiate(_moveState));
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

    private void PeelAction()
    {
        if (_nearestPeelable)
        {
            if (_nearestPeelable.PeelSeal())
            {
                //近辺のシールを探索して、プレイヤーが剥がしたシールの数だけ回収する
                Collider[] nearbyColliders = Physics.OverlapSphere(_nearestPeelable.transform.position, _peelableChainRange, _peelableLayer);
                int peelCount_new = 0;
                int peelCount_has = 0;
                foreach (var collider in nearbyColliders)
                {
                    DroppingSeal seal = collider.GetComponent<DroppingSeal>();
                    if (seal != null)
                    {
                        var result = seal.ChainPeel();
                        peelCount_new += result.newCount;
                        peelCount_has += result.hasCount;
                    }
                }

                player.AddSeal(peelCount_new,true);
                player.AddSeal(peelCount_has,false);

                player.ChangePlayerState(Instantiate(_moveState));
            }
        }
    }
}
