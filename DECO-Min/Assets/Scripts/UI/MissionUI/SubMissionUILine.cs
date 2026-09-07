using TMPro;
using UnityEngine;

public class SubMissionUILine : MonoBehaviour
{
    [SerializeField] private TMP_Text _textUI;

    private string _format;

    public void Initialize(string formatText)
    {
        _format = formatText;
    }

    public void UpdateText(params object[] args)
    {
        _textUI.text = string.Format(_format, args);
    }
}
