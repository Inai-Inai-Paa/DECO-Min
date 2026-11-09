using UnityEngine;

public class MissionClearLineUIManager : MonoBehaviour
{
    public static MissionClearLineUIManager Instance { get; private set; }

    [SerializeField] private float _viewTime = 1f;
    [SerializeField] GameObject UILine;

    private float _time = 0;

    private bool _viewFlag = false;

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
        Instance = this;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        _time = 0;
    }

    private void Update()
    {
        if (_viewFlag)
        {
            _time += Time.deltaTime;
            if (_time >= _viewTime)
            {
                UILine.SetActive(false);
                MissionUIManager.Instance.SetUIChangeFlag(true);
                MissionUIManager.Instance.UpdateUILine();
                _time = 0;
                _viewFlag = false;
            }
        }
    }

    public void MissionClearAnnounce()
    {
        UILine.SetActive(true);
        _viewFlag = true;
    }
}
