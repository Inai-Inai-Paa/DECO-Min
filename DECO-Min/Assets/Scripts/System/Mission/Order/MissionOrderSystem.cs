using UnityEngine;
using UnityEngine.InputSystem;

public class MissionOrderSystem : MonoBehaviour
{
    public static MissionOrderSystem Instance { get; private set; }

    [SerializeField] private GameObject _orderPanel;

   private bool _isActive = false;

    private void Awake()
    {
        // シングルトン
        Instance = this;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

    }
    private void Start()
    {
        _orderPanel.SetActive(false);
    }

    private void Update()
    {
        //パネルが表示されてなければリターン
        if (!_isActive) return;

        if (Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame)
        {
            OrderMission();
        }
    }

    public void OrderMission()
    {
        // ミッション受注
        Missionmanager.Instance.SetMissionStartFlg(true);
      //  MissionUIManager.Instance.SetMissionRoot(true);

        OrderPanelClose();
    }

    public void OrderPanelClose()
    {
        // パネルを閉じるアニメーションはここ
        _orderPanel.SetActive(false);
        _isActive = false;
    }

    public void OrderPanelOpen()
    {
        // パネルを開くアニメーションはここで
        _orderPanel.SetActive(true);
        _isActive = true;
    }
}
