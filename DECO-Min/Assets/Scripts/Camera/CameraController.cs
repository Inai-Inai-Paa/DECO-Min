using UnityEngine;
using UnityEngine.InputSystem;   // ← 新 Input System

/// <summary>
/// プレイヤーを追従するサードパーソンカメラ。
/// Unity Input System Package 使用。
///
/// 操作:
///   中クリック＋ドラッグ … カメラ回転（右クリックは StickerThrower 専用）
///   マウスホイール       … ズーム（StickerThrower のシール切替と競合しないよう
///                          探索中以外、または Ctrl 押しで使う設計も可）
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("追従ターゲット")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3   focusOffset = new(0f, 1.4f, 0f);

    [Header("回転感度（中クリックドラッグ）")]
    [SerializeField] private float mouseSensH = 0.3f;   // delta は pixel 単位なので小さめ
    [SerializeField] private float mouseSensV = 0.25f;

    [Header("垂直角度制限")]
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch =  60f;

    [Header("ズーム距離")]
    [SerializeField] private float defaultDistance = 8f;
    [SerializeField] private float minDistance     = 3f;
    [SerializeField] private float maxDistance     = 18f;
    [SerializeField] private float zoomSpeed       = 0.05f; // scroll は ~120/notch なので係数を小さく

    [Header("追従スムージング")]
    [SerializeField] private float positionSmoothing = 10f;
    [SerializeField] private float rotationSmoothing = 12f;

    [Header("衝突回避")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float     collisionRadius = 0.2f;

    // ─── プライベートフィールド ────────────────────────────────────────────────
    private float yaw;
    private float pitch;
    private float currentDist;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (target == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) target = pc.transform;
        }

        currentDist = defaultDistance;

        Vector3 euler = transform.eulerAngles;
        yaw   = euler.y;
        pitch = euler.x;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        HandleInput();
        ApplyCamera();
    }

    // ─── 入力（新 Input System） ──────────────────────────────────────────────
    private void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // ── 中クリックドラッグでカメラ回転 ──────────────────────────────────
        if (mouse.middleButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw   +=  delta.x * mouseSensH;
            pitch -=  delta.y * mouseSensV;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ── ホイールでズーム ─────────────────────────────────────────────────
        // 探索フェーズ中は StickerThrower がホイールをシール切替に使う。
        // 探索フェーズ以外のときだけズームを受け付けるか、
        // 両方有効にする場合は下の条件を削除する。
        bool isExploring = GameManager.Instance?.CurrentPhase == GamePhase.Exploration;
        if (!isExploring)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            currentDist -= scrollY * zoomSpeed;
            currentDist  = Mathf.Clamp(currentDist, minDistance, maxDistance);
        }
    }

    // ─── カメラ配置 ───────────────────────────────────────────────────────────
    private void ApplyCamera()
    {
        Vector3    focusPoint = target.position + focusOffset;
        Quaternion targetRot  = Quaternion.Euler(pitch, yaw, 0f);
        float      dist       = GetActualDistance(focusPoint, targetRot);
        Vector3    targetPos  = focusPoint + targetRot * new Vector3(0f, 0f, -dist);

        transform.position = Vector3.Lerp(transform.position, targetPos,
                                          positionSmoothing * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                              rotationSmoothing * Time.deltaTime);
    }

    // ─── 壁衝突回避 ──────────────────────────────────────────────────────────
    private float GetActualDistance(Vector3 focusPoint, Quaternion rot)
    {
        if (Physics.SphereCast(focusPoint, collisionRadius, rot * Vector3.back,
                               out RaycastHit hit, currentDist, collisionMask,
                               QueryTriggerInteraction.Ignore))
        {
            return Mathf.Max(hit.distance - 0.05f, minDistance);
        }
        return currentDist;
    }

    public void SetTarget(Transform t) => target = t;
}
