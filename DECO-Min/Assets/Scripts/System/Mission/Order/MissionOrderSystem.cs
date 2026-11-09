using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MissionOrderSystem : MonoBehaviour
{
    public static MissionOrderSystem Instance { get; private set; }

    [SerializeField] private GameObject _orderPanel;
    [SerializeField] private List<TMP_Text> texts;
    [SerializeField] private MIssionPresets mIssion;

    [SerializeField] private GameObject selectIcon;

    [SerializeField] private GameObject[] buttonObj; 
 
    private inp_test inputAction;

    // 仮実装
    private int _select = 0;

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

        for (int i = 0; i < texts.Count; i++)
        {
            texts[i].text = mIssion.missions[i].missionText;
        }
    }

    private void Update()
    {

        //パネルが表示されてなければリターン
        if (!_isActive) return;


        if (Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame)
        {
            //   OrderMission();
            if (_select == 0)
            {
                OrderMission();
            }
            else
            {
                OrderPanelClose();

            }

        }

        if (Keyboard.current != null &&
            Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
           // OrderPanelClose();
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            _select++;
            _select %= 2;
        }
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            _select--;
            _select *= -1;
        }
        selectIcon.transform.position = buttonObj[_select].transform.position;
    }

    public void OrderMission()
    {
        // ミッション受注
        Missionmanager.Instance.SetMissionStartFlg(true);
        MissionUIManager.Instance.SetMissionRoot(true);
        MissionUIManager.Instance.UpdateUILine();

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
