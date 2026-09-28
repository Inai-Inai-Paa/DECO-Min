using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Player/Peel")]
public class PlayerPeel : PlayerState
{
    [Header("TransitionState")]
    [Tooltip("MoveState"),SerializeField] private PlayerState _moveState;

    [Header("Options")]
    [Tooltip("Peel�s�����������Ƃ��̍d������"), SerializeField] private float _successDuration = 0.3f;
    [Tooltip("Peel�s�����s�����Ƃ��̍d������"), SerializeField] private float _failDuration = 0.2f;
    [Tooltip("Peel��I�u�W�F�N�g�̃^�O"), SerializeField] private string _peelableTag = "Peelable";
    [Tooltip("Peel��I�u�W�F�N�g�̃��C���["), SerializeField] private LayerMask _peelableLayer;
    [Tooltip("Peel�s���̒T�m�͈�"), SerializeField] private float _peelableRange = 0.5f;
    public float GetPeelableRange() { return _peelableRange; }

    [Tooltip("Peel�������ɂ܂Ƃ܂��Ĕ������͈�"), SerializeField] private float _peelableChainRange = 2.0f;
    public float GetPeelableChainRange() { return _peelableChainRange; }

    [Tooltip("���Z�����V�[���̐�"), SerializeField] private int _addSealCount = 1;

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
                //�ł��߂��Ώۂ��擾����
                var currentDistance = _nearestPeelable != null ? Vector3.Distance(player.transform.position, _nearestPeelable.transform.position) : float.MaxValue;
                var newDistance = Vector3.Distance(player.transform.position, collider.transform.position);
                if (_nearestPeelable == null || newDistance < currentDistance)
                {
                    //�Ώۂ�DroppingSeal�ł��邱�Ƃ��ēx�m�F����
                    if (collider.gameObject.GetComponent<DroppingSeal>() != null)
                    {
                        _nearestPeelable = collider.gameObject.GetComponent<DroppingSeal>();
                        
                    }  
                }
            }
        }

        if (_nearestPeelable != null)
        {
            player.ResetPeelCooldown(); //�����s���ɐ��������ꍇ��CD�̃��Z�b�g
            PeelAction(); //�ŏ���1��͔������A�N�V�����������ōs��
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
            //�v���C���[�ړ����͂��擾������_moveState�ɑJ�ڂ���A�������d���������ɖ������Ă��邱�Ƃ��m�F����
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
                //�ߕӂ̃V�[����T�����āA�v���C���[�����������V�[���̐������������
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
