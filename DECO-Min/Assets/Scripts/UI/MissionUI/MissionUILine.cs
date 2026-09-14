using TMPro;
using UnityEngine;

public class MissionUILine : MonoBehaviour
{
    [SerializeField] private TMP_Text _textUI;
    [SerializeField] private TMP_Text _numbertextUI;

    private string _format;

    public void Initialize(string formatText)
    {
        _format = formatText;
    }

    public void UpdateText(params object[] args)
    {
        _textUI.text = string.Format(_format, args);
    }
    
    public void UpdateNumberText(params object[] args)
    {
        _numbertextUI.text = string.Format(_format, args);
    }
}
