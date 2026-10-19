#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CrabBoss2_0))]
public class CrabBoss2Editor : Editor
{
    private SerializedProperty leftHandState;
    private SerializedProperty rightHandState;
    private SerializedProperty bodyHealth;

    private void OnEnable()
    {
        leftHandState = serializedObject.FindProperty("leftHandState");
        rightHandState = serializedObject.FindProperty("rightHandState");
        bodyHealth = serializedObject.FindProperty("bodyHealth");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CrabBoss2_0 boss = (CrabBoss2_0)target;
        EditorGUILayout.LabelField("Boss State", boss.CurrentState.ToString());
        EditorGUILayout.LabelField("Activation", boss.IsActive ? "Active" : "Inactive");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hand Debug Controls", EditorStyles.boldLabel);
        DrawHandControls("Left Hand", leftHandState, boss => boss.DamageLeftHand(25), boss => boss.DestroyLeftHand(), boss => boss.RestoreLeftHand());
        DrawHandControls("Right Hand", rightHandState, boss => boss.DamageRightHand(25), boss => boss.DestroyRightHand(), boss => boss.RestoreRightHand());
        DrawBodyControls(boss);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBodyControls(CrabBoss2_0 boss)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Body Health", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Damage Gate", boss.CanDamageBody ? "Unlocked" : "Destroy both hands first");
        EditorGUILayout.PropertyField(bodyHealth, includeChildren: true);
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(!boss.CanDamageBody))
        {
            if (GUILayout.Button("Damage 25"))
            {
                ApplyToTargets("Damage Boss Body", targetBoss => targetBoss.DamageBody(25));
            }
        }
        if (GUILayout.Button("Restore"))
        {
            ApplyToTargets("Restore Boss Body", targetBoss => targetBoss.RestoreBodyHealth());
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawHandControls(string label, SerializedProperty handState, Action<CrabBoss2_0> damage, Action<CrabBoss2_0> destroy, Action<CrabBoss2_0> restore)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(handState, includeChildren: true);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Damage 25"))
        {
            ApplyToTargets("Damage " + label, damage);
        }
        if (GUILayout.Button("Destroy"))
        {
            ApplyToTargets("Destroy " + label, destroy);
        }
        if (GUILayout.Button("Restore"))
        {
            ApplyToTargets("Restore " + label, restore);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void ApplyToTargets(string undoName, Action<CrabBoss2_0> action)
    {
        foreach (UnityEngine.Object selectedTarget in targets)
        {
            CrabBoss2_0 boss = (CrabBoss2_0)selectedTarget;
            Undo.RecordObject(boss, undoName);
            action(boss);
            EditorUtility.SetDirty(boss);
        }
    }
}
#endif
