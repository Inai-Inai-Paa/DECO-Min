using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static LimitMissionPreset;

[CustomPropertyDrawer(typeof(LimitMissionPreset))]
public class MissionDrower : PropertyDrawer
{
    private const float _lineHeight = 20f;
    private const float _space = 4f;
    private const float _labelWidth = 120f;
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines = 0;
        
        SerializedProperty missiontype = property.FindPropertyRelative(nameof(LimitMissionPreset.missionType));

        lines += 4;
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

        SerializedProperty missionType = property.FindPropertyRelative(nameof(LimitMissionPreset.missionType));
        SerializedProperty objectCount = property.FindPropertyRelative(nameof(LimitMissionPreset.objectCount));
        SerializedProperty targetObject = property.FindPropertyRelative(nameof(LimitMissionPreset.targetObject));
        SerializedProperty timeLimit = property.FindPropertyRelative(nameof(LimitMissionPreset.timeLimit));
        SerializedProperty rewardObject = property.FindPropertyRelative(nameof(LimitMissionPreset.reward));
        SerializedProperty rewardSeal = property.FindPropertyRelative(nameof(LimitMissionPreset.rewardSeal));
        Rect r = new Rect(position.x, position.y, position.width, _lineHeight);
        DrawRow(ref r, "ミッション方式", missionType);
        
        switch((MissionTpye)missionType.enumValueIndex)
        {
            case MissionTpye.Defeat:
                DrawRow(ref r, "対象", targetObject);
                DrawRow(ref r, "必要数", objectCount);
                break;
            case MissionTpye.Collect:

                DrawRow(ref r, "対象", targetObject);
                DrawRow(ref r, "必要数", objectCount);
                break;
            case MissionTpye.Place:
                DrawRow(ref r, "対象エリア", targetObject);
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
