using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private float _speed;
    [SerializeField] private float _duration;//減速
    [SerializeField] private float _dropVelocity;//落下速度
    [SerializeField] private Vector3 _direction;

    void Start()
    {
        //XとYを参照して角度を決める
    }

    void Update()
    {
        

        transform.position += _direction * _speed;

        //毎フレーム減速させる
        _speed -= _duration * Time.deltaTime;

        //どんどん落ちるスピードを速くする 倍率は後で決めよう
        _dropVelocity += _dropVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //地面に当たったらシール化 
        if (collision.gameObject.CompareTag("default"))
        {
            //シールが出来るまではデストロイ！！
            Destroy(gameObject);
        }
    }
}
