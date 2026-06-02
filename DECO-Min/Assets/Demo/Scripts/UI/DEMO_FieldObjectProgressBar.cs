using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FieldObject の上に表示されるワールドスペースのシール化進捗バー。
/// Canvas を World Space に設定したプレハブにアタッチして使用する。
/// </summary>
public class DEMO_FieldObjectProgressBar : MonoBehaviour
{
    [Header("UI 要素")]
    [SerializeField] private Slider          slider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image           fillImage;

    [Header("カラーグラデーション")]
    [SerializeField] private Color colorLow  = Color.red;
    [SerializeField] private Color colorHigh = Color.green;

    [Header("常にカメラを向く")]
    [SerializeField] private bool billboardMode = true;

    private Transform mainCam;

    private void Start()
    {
        if (Camera.main != null) mainCam = Camera.main.transform;
        if (slider != null) slider.value = 0f;
    }

    private void LateUpdate()
    {
        if (!billboardMode || mainCam == null) return;
        // カメラの方向を向く（ビルボード）
        transform.rotation = Quaternion.LookRotation(transform.position - mainCam.position);
    }

    /// <summary>進捗を更新する。FieldObject.UpdateProgressBar() から呼ばれる。</summary>
    public void UpdateProgress(int current, int max)
    {
        float t = max > 0 ? (float)current / max : 0f;

        if (slider != null) slider.value = t;

        if (fillImage != null) fillImage.color = Color.Lerp(colorLow, colorHigh, t);

        if (progressText != null) progressText.text = $"{current}/{max}";
    }
}
