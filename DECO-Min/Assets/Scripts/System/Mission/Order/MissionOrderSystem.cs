using UnityEngine;

public class MissionOrderSystem : MonoBehaviour
{
    public static MissionOrderSystem Instance { get; private set; }

    [SerializeField] private GameObject _orderPanel;
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

    public void OrderMission()
    {
        // ミッション受注
        Missionmanager.Instance.SetMissionStartFlg(true);
        MissionUIManager.Instance.SetMissionRoot(true);

        OrderPanelClose();
    }

    public void OrderPanelClose()
    {
        // パネルを閉じるアニメーションはここ
        _orderPanel.SetActive(false);
    }

    public void OrderPanelOpen()
    {
        // パネルを開くアニメーションはここで
        _orderPanel.SetActive(true);
    }
}
