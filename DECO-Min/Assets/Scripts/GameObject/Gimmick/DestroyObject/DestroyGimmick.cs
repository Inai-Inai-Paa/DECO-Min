using UnityEngine;

public class DestroyGimmick : Gimmick
{
    [Header("壊すオブジェクト")]
    [SerializeField] public GameObject _target;

    [Header("UI")]
    [SerializeField] public GameObject _interactUI;

    [Header("ステート")]
    [SerializeField] public GimmickState _passiveState;
    [SerializeField] public GimmickState _activeState;

    public bool _playerInside { get; private set; } = false;
    public Player Player => _player;

    protected override void Start()
    {
        base.Start();
        ChangeGimmickState(Instantiate(_passiveState));
        _target.SetActive(true);
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
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (_player == player)
            {
                _player = null;
                _playerInside = false;
            }
        }
    }
}
