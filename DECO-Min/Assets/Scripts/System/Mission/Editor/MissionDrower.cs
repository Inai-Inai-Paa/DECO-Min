using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static MissionPreset;

[CustomPropertyDrawer(typeof(MissionPreset))]
public class MissionDrower : PropertyDrawer
{
    private const float _lineHeight = 20f;
    private const float _space = 4f;
    private const float _labelWidth = 120f;
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines = 0;
        
        SerializedProperty missiontype = property.FindPropertyRelative(nameof(MissionPreset.missionType));

        lines += 5;
        switch ((MissionTpye)missiontype.enumValueIndex)
        {
            case MissionTpye.Defeat:
                lines += 1;
                break;
            case MissionTpye.Collect:
                lines += 1;
                break;
            case MissionTpye.Place:
                break;
        }
        var type = (MissionTpye)missiontype.enumValueIndex;
        return lines * (_lineHeight + _space);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty missionText = property.FindPropertyRelative(nameof(MissionPreset.missionText));
        SerializedProperty missionType = property.FindPropertyRelative(nameof(MissionPreset.missionType));
        SerializedProperty objectCount = property.FindPropertyRelative(nameof(MissionPreset.objectCount));
        SerializedProperty killTarget = property.FindPropertyRelative(nameof(MissionPreset.killObject));
        SerializedProperty Collecttarget = property.FindPropertyRelative(nameof(MissionPreset.collectObject));
        SerializedProperty timeLimit = property.FindPropertyRelative(nameof(MissionPreset.timeLimit));
        SerializedProperty rewardObject = property.FindPropertyRelative(nameof(MissionPreset.reward));
        SerializedProperty rewardSeal = property.FindPropertyRelative(nameof(MissionPreset.rewardSeal));
        Rect r = new Rect(position.x, position.y, position.width, _lineHeight);

        DrawRow(ref r, "ミッションテキスト", missionText);

        DrawRow(ref r, "ミッション方式", missionType);
        
        switch((MissionTpye)missionType.enumValueIndex)
        {
            case MissionTpye.Defeat:
                DrawRow(ref r, "対象", killTarget);
                DrawRow(ref r, "必要数", objectCount);
                break;
            case MissionTpye.Collect:

                DrawRow(ref r, "対象", Collecttarget);
                DrawRow(ref r, "必要数", objectCount);
                break;
            case MissionTpye.Place:
                break;

        }
        DrawRow(ref r, "報酬 (ぼんどろ)", rewardObject);
        DrawRow(ref r, "報酬 (シール枚数)", rewardSeal);

        EditorGUI.EndProperty();
    }

    private void DrawRow(ref Rect rowRect, string label, SerializedProperty property)
    {
        Rect labelRect = new Rect(rowRect.x, rowRect.y, _labelWidth, _lineHeight);
        Rect fieldRect = new Rect(rowRect.x + _labelWidth + 4f, rowRect.y, rowRect.width - _labelWidth - 4f, _lineHeight);

        EditorGUI.LabelField(labelRect, label);
        EditorGUI.PropertyField(fieldRect, property, GUIContent.none);

        rowRect.y += _lineHeight + _space;
    }
}
