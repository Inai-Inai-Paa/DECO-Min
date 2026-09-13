using UnityEngine;
using UnityEngine.XR;

public class GimmickBridge : Gimmick
{
    [Header("現れるオブジェクト")]
    [SerializeField] public GameObject _bridge;

    [Header("UI")]
    [SerializeField] public GameObject _interactUI;

    [Header("ステート")]
    [SerializeField] public GimmickState _waitState;
    [SerializeField] public GimmickState _readyState;

    public bool _playerInside { get; private set; } = false;
    public Player Player => _player;

    protected override void Start()
    {
        base.Start();
        ChangeGimmickState(Instantiate(_waitState));
        _bridge.SetActive(false);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _player = other.GetComponent<Player>();
            _playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if(_player == player)
            {
                _player = null;
                _playerInside = false;
            }
        }
    }
}
