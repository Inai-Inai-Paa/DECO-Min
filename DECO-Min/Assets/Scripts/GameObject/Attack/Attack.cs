using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private float _speed = 1.0f;
    [SerializeField] private float _duration = 0.1f;//減速
    [SerializeField] private float _dropVelocity = 0.001f;//落下速度
    private Vector3 _direction;

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
        _speed *= _duration;

        transform.position -= new Vector3(0.0f, _dropVelocity, 0.0f) * Time.deltaTime;

        //どんどん落ちるスピードを速くする 倍率は後で決めよう
        _dropVelocity += _dropVelocity * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //地面に当たったらシール化 
        //if (collision.gameObject.CompareTag("Untagged"))
        //{
        //    //シールが出来るまではデストロイ！！
        //    Destroy(gameObject);
        //}
    }
}
