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
        
        lines += 3;

        var type = (MissionTpye)missiontype.enumValueIndex;
        return lines * (_lineHeight + _space);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty missionType = property.FindPropertyRelative(nameof(MissionPreset.missionType));
        SerializedProperty objectCount = property.FindPropertyRelative(nameof(MissionPreset.objectCount));
        SerializedProperty targetObject = property.FindPropertyRelative(nameof(MissionPreset.targetObject));
        Rect r = new Rect(position.x, position.y, position.width, _lineHeight);
        DrawRow(ref r, "ミッション方式", missionType);
        
        DrawRow(ref r, "必要数", objectCount);
        DrawRow(ref r, "対象", targetObject);



        //switch((MissionPreset.MissionTpye)missionType.enumValueIndex)
        //{

        //}

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
