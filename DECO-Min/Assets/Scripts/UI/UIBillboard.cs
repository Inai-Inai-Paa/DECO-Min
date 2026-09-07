using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    [Header("UI Billboard Settings")]
    [SerializeField] private Camera _mainCamera;

    void Start()
    {
        if(_mainCamera == null )
        {
            _mainCamera = Camera.main;
        }
    }

    void Update()
    {
        transform.rotation = _mainCamera.transform.rotation;
    }
}
