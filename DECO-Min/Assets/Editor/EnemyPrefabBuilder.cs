using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyPrefabBuilder
{
    private const string Folder = "Assets/Resources/Prefab/Enemy";
    private const string TacklePath = Folder + "/TackleEnemy.prefab";
    private const string RangedPath = Folder + "/RangedEnemy.prefab";
    private const string TackleSourcePath = Folder + "/testEnemy.prefab";
    private const string RangedSourcePath = Folder + "/Mortar/MortarTurret.prefab";
    private const string StatusPath = "Assets/Scripts/GameObject/Character/Enemy/Status/DefaultEnemyStatus.asset";
    private const string ScenePath = "Assets/Scenes/HayakawaTestScene.unity";
    private const string RequestFileName = "build-enemy-prefabs.txt";

    [MenuItem("DECO-Min/Create Enemy Prefabs")]
    public static void Create()
    {
        EnemyStatus status = AssetDatabase.LoadAssetAtPath<EnemyStatus>(StatusPath);
        SaveIndependentPrefab(TackleSourcePath, "TackleEnemy", TacklePath, status);
        SaveIndependentPrefab(RangedSourcePath, "RangedEnemy", RangedPath, status);
        GameObject tackle = AssetDatabase.LoadAssetAtPath<GameObject>(TacklePath);
        GameObject ranged = AssetDatabase.LoadAssetAtPath<GameObject>(RangedPath);
        RetargetScene(tackle, ranged);
        AssetDatabase.SaveAssets();
        Debug.Log("Enemy prefabs are ready in " + Folder);
    }

    [InitializeOnLoadMethod]
    private static void ScheduleAutoBuild()
    {
        if (!File.Exists(RequestPath()))
        {
            return;
        }

        EditorApplication.delayCall += AutoBuild;
    }

    private static void AutoBuild()
    {
        if (!File.Exists(RequestPath()))
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= BuildAfterPlayMode;
            EditorApplication.playModeStateChanged += BuildAfterPlayMode;
            return;
        }

        File.Delete(RequestPath());

        try
        {
            Create();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void BuildAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= BuildAfterPlayMode;
        EditorApplication.delayCall += () =>
        {
            try
            {
                Create();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    private static string RequestPath()
    {
        return Path.Combine(Application.dataPath, "Editor", RequestFileName);
    }

    private static bool SaveIndependentPrefab(
        string sourcePath,
        string prefabName,
        string destinationPath,
        EnemyStatus status)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath) != null)
        {
            return false;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            throw new InvalidOperationException("Missing enemy source prefab: " + sourcePath);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = prefabName;

        Enemy enemy = instance.GetComponent<Enemy>();
        if (enemy != null && status != null)
        {
            SerializedObject data = new SerializedObject(enemy);
            data.FindProperty("characterStatus").objectReferenceValue = status;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(instance, destinationPath);
        UnityEngine.Object.DestroyImmediate(instance);
        return true;
    }

    private static void RetargetScene(GameObject tacklePrefab, GameObject rangedPrefab)
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded || tacklePrefab == null || rangedPrefab == null)
        {
            return;
        }

        List<GameObject> roots = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Enemy[] enemies = root.GetComponentsInChildren<Enemy>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (PrefabUtility.GetNearestPrefabInstanceRoot(enemies[i].gameObject) == enemies[i].gameObject)
                {
                    roots.Add(enemies[i].gameObject);
                }
            }
        }

        bool changed = false;
        for (int i = 0; i < roots.Count; i++)
        {
            GameObject current = roots[i];
            if (current == null)
            {
                continue;
            }

            Enemy enemy = current.GetComponent<Enemy>();
            GameObject replacement = enemy is TackleEnemy ? tacklePrefab : enemy is RangedEnemy ? rangedPrefab : null;
            if (replacement == null)
            {
                continue;
            }

            GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(current);
            if (source == replacement)
            {
                continue;
            }

            Transform parent = current.transform.parent;
            Vector3 position = current.transform.position;
            Quaternion rotation = current.transform.rotation;
            string objectName = current.name;
            UnityEngine.Object.DestroyImmediate(current);

            GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(replacement, parent);
            created.name = objectName;
            created.transform.SetPositionAndRotation(position, rotation);
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
