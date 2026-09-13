using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private float _speed = 1.0f;
    [SerializeField] private float _duration = 0.1f;//減速
    [SerializeField] private float _dropVelocity = 0.001f;//落下速度
    private Vector3 _direction;

    [SerializeField] private GameObject _sealObject;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _rayLength = 0.3f;

    void Start()
    {
        //XとYを参照して角度を決める
        Quaternion rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0.0f);
        _direction = rotation * Vector3.forward;
    }

    void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        //毎フレーム減速させる
        _speed *= Mathf.Pow(_duration, Time.deltaTime * 60f);

        transform.position -= new Vector3(0.0f, _dropVelocity, 0.0f) * Time.deltaTime;

        //どんどん落ちるスピードを速くする 倍率は後で決めよう
        _dropVelocity += _dropVelocity * Time.deltaTime;

        CheckGround();
    }

    /// <summary>
    /// レイを飛ばして地面判定を行う
    /// 地面ならドロップシールに変えて死亡！
    /// </summary>
    void CheckGround()
    {
        //着地判定を取って落ちたらシール化
        if (Physics.Raycast(transform.position, Vector3.down, _rayLength, _groundLayer))
        {
            GameObject dropSeal = Instantiate(_sealObject, transform.position, Quaternion.identity);
            dropSeal.GetComponent<DroppingSeal>().SetCreateSource(SealCreateSource.Player);
            Destroy(gameObject);
        }
    }
}
