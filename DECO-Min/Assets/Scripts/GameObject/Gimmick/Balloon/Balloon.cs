using Unity.VisualScripting;
using UnityEngine;

public class Balloon : Gimmick
{
    [Header("Balloon Settings")]
    [SerializeField] private float _MaxHeight = 10.0f;
    [SerializeField] private float _MoveSpeed = 2.0f;

    private bool _isPlayerOnPlate = false;
    public void SetOnPlate(bool value)
    {
       _isPlayerOnPlate = value;
    }
    private bool _isActive = false;
    private Vector3 _defaultPos;

    protected override void Start()
    {
        base.Start();

        if(_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        }

        _defaultPos = transform.position;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if(_isPlayerOnPlate)
        {
            if (_player != null && _player.playerInputData.InteractPressed)
            {
                _isActive = !_isActive;
            }
        }
        else
        {
            _isActive = false;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if(_isActive)
        {
            if(transform.position.y < _MaxHeight)
            {
                transform.position += Vector3.up * Time.fixedDeltaTime * _MoveSpeed;
            }
        }
        else
        {
            if(transform.position.y > _defaultPos.y)
            {
                transform.position -= Vector3.up * Time.fixedDeltaTime * _MoveSpeed;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * _MaxHeight);
    }

}
