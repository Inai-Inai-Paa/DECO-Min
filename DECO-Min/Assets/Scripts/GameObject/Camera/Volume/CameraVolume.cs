using UnityEngine;

public sealed class CameraVolume : MonoBehaviour
{
    [System.Serializable]
    public struct CameraParams
    {
        [Tooltip("Originから見たカメラのローカル位置")]
        public Vector3 offset;

        [Range(1f, 179f)]
        public float fieldOfView;

        [Min(0f)]
        public float smoothness;

        [Tooltip(
            "各軸の最小操作角度。\n" +
            "値は-180～180度で指定します。")]
        public Vector3 rotationLimitMin;

        [Tooltip(
            "各軸の最大操作角度。\n" +
            "Min=-180、Max=180の軸は完全自由になります。")]
        public Vector3 rotationLimitMax;
    }

    [Header("Volume Settings")]

    [Min(0.01f)]
    [SerializeField]
    private float _radius = 20f;

    [Tooltip("Volumeが重なった場合の選択優先度")]
    [SerializeField]
    private int _priority;

    [Header("Camera State")]

    [SerializeField]
    private CameraState _stateTemplate;

    [Header("Camera Settings")]

    [SerializeField]
    private CameraParams _config =
        new CameraParams
        {
            offset =
                new Vector3(
                    0f,
                    5f,
                    -10f),

            fieldOfView =
                60f,

            smoothness =
                5f,

            rotationLimitMin =
                new Vector3(
                    -5f,
                    -15f,
                    0f),

            rotationLimitMax =
                new Vector3(
                    45f,
                    15f,
                    0f)
        };

    private CameraController _controller;
    private CameraState _runtimeState;

    public float Radius =>
        _radius;

    public int Priority =>
        _priority;

    public CameraParams Config =>
        _config;

    /// <summary>
    /// Inspectorに設定されたCameraStateアセット。
    /// </summary>
    public CameraState StateTemplate =>
        _stateTemplate;

    /// <summary>
    /// 実行時にVolume専用として複製されたCameraState。
    /// </summary>
    public CameraState RuntimeState =>
        _runtimeState;

    /// <summary>
    /// CameraControllerから呼び出される初期化。
    /// </summary>
    public void Initialize(
        CameraController controller)
    {
        ReleaseRuntimeState();

        _controller =
            controller;

        if (_controller == null)
        {
            Debug.LogError(
                $"{name}: CameraControllerがnullです。",
                this);

            return;
        }

        if (_stateTemplate == null)
        {
            Debug.LogWarning(
                $"{name}: CameraStateが設定されていません。",
                this);

            return;
        }

        /*
         * Stateアセットを直接使用すると、
         * 複数Volume間で実行時参照が共有される。
         *
         * Volumeごとに専用インスタンスを生成する。
         */
        _runtimeState =
            Instantiate(
                _stateTemplate);

        _runtimeState.name =
            $"{_stateTemplate.name} Runtime ({name})";

        _runtimeState.hideFlags =
            HideFlags.HideAndDontSave;

        _runtimeState.Initialize(
            _controller,
            _controller.ControlledCamera,
            _controller.StateMachine,
            _controller.Target,
            this);
    }

    /// <summary>
    /// 指定されたワールド座標がVolume内に存在するか判定する。
    /// Colliderは使用しない。
    /// </summary>
    public bool Contains(
        Vector3 worldPosition)
    {
        if (_radius <= Mathf.Epsilon)
        {
            return false;
        }

        Vector3 difference =
            worldPosition -
            transform.position;

        return
            difference.sqrMagnitude <=
            _radius * _radius;
    }

    /// <summary>
    /// Volume中心から指定座標までの距離の二乗。
    /// </summary>
    public float GetSquaredDistance(
        Vector3 worldPosition)
    {
        Vector3 difference =
            worldPosition -
            transform.position;

        return
            difference.sqrMagnitude;
    }

    /// <summary>
    /// CameraVolume直下の地面位置を取得する。
    ///
    /// Volumeへの進入判定にはColliderを使わないが、
    /// GroundRelativeとEditorプレビューでは
    /// 地面検出用としてPhysics.Raycastを使用する。
    /// </summary>
    public bool GetGroundPosition(
        out Vector3 groundPosition)
    {
        groundPosition =
            transform.position;

        const float maxDistance =
            500f;

        if (!Physics.Raycast(
                transform.position,
                Vector3.down,
                out RaycastHit hit,
                maxDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        groundPosition =
            hit.point;

        return true;
    }

    public bool UsesState<T>()
        where T : CameraState
    {
        return
            _runtimeState is T;
    }

    /// <summary>
    /// 実行時に生成したCameraStateを破棄する。
    /// </summary>
    public void ReleaseRuntimeState()
    {
        if (_runtimeState != null)
        {
            if (Application.isPlaying)
            {
                Destroy(
                    _runtimeState);
            }
            else
            {
                DestroyImmediate(
                    _runtimeState);
            }

            _runtimeState =
                null;
        }

        _controller =
            null;
    }

    private void OnDestroy()
    {
        ReleaseRuntimeState();
    }

    private void OnValidate()
    {
        _radius =
            Mathf.Max(
                0.01f,
                _radius);

        CameraParams config =
            _config;

        config.fieldOfView =
            Mathf.Clamp(
                config.fieldOfView,
                1f,
                179f);

        config.smoothness =
            Mathf.Max(
                0f,
                config.smoothness);

        config.rotationLimitMin =
            ClampRotationLimit(
                config.rotationLimitMin);

        config.rotationLimitMax =
            ClampRotationLimit(
                config.rotationLimitMax);

        SortMinMax(
            ref config.rotationLimitMin.x,
            ref config.rotationLimitMax.x);

        SortMinMax(
            ref config.rotationLimitMin.y,
            ref config.rotationLimitMax.y);

        SortMinMax(
            ref config.rotationLimitMin.z,
            ref config.rotationLimitMax.z);

        _config =
            config;
    }

    private static Vector3 ClampRotationLimit(
        Vector3 value)
    {
        value.x =
            Mathf.Clamp(
                value.x,
                -180f,
                180f);

        value.y =
            Mathf.Clamp(
                value.y,
                -180f,
                180f);

        value.z =
            Mathf.Clamp(
                value.z,
                -180f,
                180f);

        return value;
    }

    private static void SortMinMax(
        ref float minimum,
        ref float maximum)
    {
        if (minimum <= maximum)
        {
            return;
        }

        float temporary =
            minimum;

        minimum =
            maximum;

        maximum =
            temporary;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(
                0f,
                _radius));
    }
}