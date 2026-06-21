using UnityEngine;

public class DroppingSeal : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Seal Settings")]
    [Tooltip("剥がすために必要なゲージ量"), SerializeField] private int _requiredGauge = 5;
    [Tooltip("ゲージの表示時間"), SerializeField] private float _gaugeDisplayTime = 2.0f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
