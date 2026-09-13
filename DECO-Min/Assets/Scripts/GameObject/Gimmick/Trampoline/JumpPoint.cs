using UnityEngine;

public class JumpPoint : MonoBehaviour
{
    [Header("Parent Object")]
    [Tooltip("トランポリン本体"), SerializeField] private GameObject _parentObject;

    [Header("Jump Point Setting")]
    [Tooltip("スタートポイントorエンドポイント"), SerializeField] private bool _isStartPoint;

    private Trampoline _trampoline;
    void Start()
    {
        if(_parentObject == null)
        {
            _parentObject = transform.parent.gameObject;
            if(_parentObject == null)
            {
                Debug.LogError("Parent Objectが設定されていません。");
            }
        }
        _trampoline = _parentObject.GetComponent<Trampoline>();
        if(_trampoline == null)
        {
            Debug.LogError("Trampolineコンポーネントが見つかりません。");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _trampoline.ChangeUIActive(true, _isStartPoint);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _trampoline.ChangeUIActive(false);
        }
    }
}
