using UnityEngine;
using UnityEngine.UI;

public class DroppingSeal : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Seal Settings")]
    [Tooltip("剥がすために必要なゲージ量"), SerializeField] private int _requiredGauge = 5;
    [Tooltip("ゲージの表示時間"), SerializeField] private float _gaugeDisplayTime = 2.0f;
    [Tooltip("ゲージの表示用オブジェクト"), SerializeField] private Slider _gaugeObject;
    [Tooltip("生成元"), SerializeField] private SealCreateSource _createSource = SealCreateSource.Enemy;
    public void SetCreateSource(SealCreateSource source){ _createSource = source; }
    public SealCreateSource GetCreateSource(){ return _createSource; }

    [Tooltip("連鎖剥離の範囲"), SerializeField] private float _chainPeelRange = 1.0f;
    [Tooltip("Peel先オブジェクトのレイヤー"), SerializeField] private LayerMask _peelableLayer;

    private int _currentGauge = 0;
    private float _currentDisplayTime = 0.0f;

    private bool _isPeeled = false;
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
        _isPeeled = false;
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
            return true; // 剥がし成功
        }

        return false; // 剥がし失敗
    }

    public (int newCount, int hasCount) ChainPeel()
    {
        int newCount = 0;
        int hasCount = 0;

        if(_isPeeled)
        {
            return (newCount, hasCount); // 既に剥がされている場合は処理を中断
        }

        _isPeeled = true;

        // 連鎖剥離の処理
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _chainPeelRange, _peelableLayer);
        foreach (var hitCollider in hitColliders)
        {
            DroppingSeal seal = hitCollider.GetComponent<DroppingSeal>();
            if (seal != null && seal != this) // 自分自身は除外
            {
                var result = seal.ChainPeel();
                newCount += result.newCount;
                hasCount += result.hasCount;
            }
        }

        if(_createSource == SealCreateSource.Player)
        {
            hasCount++; // プレイヤー由来のSealを剥がした場合はhasCountを増やす
        }
        else
        {
            newCount++; // 敵由来のSealを剥がした場合はnewCountを増やす
        }

            Destroy(gameObject); // Sealを破壊

        return (newCount, hasCount);
    }

    private void OnDestroy()
    {
        // Sealが破壊されたときの処理
        if (_gaugeObject != null)
        {
            _gaugeObject.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 連鎖剥離の範囲を可視化
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _chainPeelRange);
    }
}
