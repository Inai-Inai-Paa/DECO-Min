using UnityEngine;
using UnityEngine.UI;

public class DroppingSeal : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Seal Settings")]
    [Tooltip("剥がすために必要なゲージ量"), SerializeField] private int _requiredGauge = 5;
    [Tooltip("ゲージの表示時間"), SerializeField] private float _gaugeDisplayTime = 2.0f;
    [Tooltip("ゲージの表示用オブジェクト"), SerializeField] private Slider _gaugeObject;

    int _currentGauge = 0;
    float _currentDisplayTime = 0.0f;
    void Start()
    {
        _currentGauge = _requiredGauge;
        
        if (_gaugeObject != null)
        {
            _gaugeObject.maxValue = _requiredGauge;
            _gaugeObject.value = _currentGauge;
        }
        else
        {
            Debug.LogWarning("Gauge object is not assigned in the inspector.");
        }

        _currentDisplayTime = 0.0f;
        _gaugeObject.gameObject.SetActive(false); // 初期状態では非表示
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if (_gaugeObject != null && _gaugeObject.gameObject.activeSelf)
        {
            _currentDisplayTime -= Time.fixedDeltaTime;
            if (_currentDisplayTime <= 0.0f)
            {
                _gaugeObject.gameObject.SetActive(false);
                _currentDisplayTime = 0.0f;
            }
        }
    }
    public bool PeelSeal()
    {
        _currentGauge--;
        _gaugeObject.value = _currentGauge;
        _currentDisplayTime = _gaugeDisplayTime; // ゲージ表示時間をリセットして再表示
        _gaugeObject.gameObject.SetActive(true); // ゲージを表示
        if (_currentGauge <= 0)
        {
            Destroy(gameObject);    // 一旦削除
            return true; // 剥がし成功
        }

        return false; // 剥がし失敗
    }
}
