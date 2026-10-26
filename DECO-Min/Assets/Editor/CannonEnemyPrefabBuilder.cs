using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CannonEnemyPrefabBuilder
{
    private const string Folder = "Assets/Resources/Prefab/Enemy/";
    private const string Models = "Assets/Resources/Model/cannon_mdl/";
    private const string EnemyPath = Folder + "CannonEnemy.prefab";
    private const string ShellPath = Folder + "CannonShell.prefab";
    private const float ModelScale = 0.5f;

    [MenuItem("DECO-Min/Create Cannon Enemy")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Create Cannon Enemy in Edit Mode.");

        // Existing authored prefabs are never overwritten by the creation command.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ShellPath) == null)
            CreateShell();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath) == null)
            CreateEnemy();
        AssetDatabase.SaveAssets();
        CannonEnemyValidation.Validate();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
        PrefabStageUtility.OpenPrefab(EnemyPath);
        Debug.Log("CannonEnemy and CannonShell are ready in " + Folder);
    }

    private static void CreateEnemy()
    {
        var root = PrefabUtility.LoadPrefabContents(Folder + "RangedEnemy.prefab");
        try
        {
            root.name = "CannonEnemy";
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            var body = AddModel("nannon_body_mdl", root.transform, ModelScale);
            body.name = "CannonBase";
            AddHitBox(body);

            var head = AddModel("cannon_head_mdl", root.transform, ModelScale);
            head.name = "CannonHead";
            var headBounds = BoundsOf(head);
            // The FBX head origin is the authored center of the spherical swivel.
            var pivot = new GameObject("TurretPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.position = head.transform.position;
            head.transform.SetParent(pivot, true);
            AddHitBox(head);

            var muzzle = new GameObject("FirePoint").transform;
            muzzle.SetParent(pivot, false);
            muzzle.position = new Vector3(headBounds.center.x, pivot.position.y, headBounds.max.z + 0.02f);

            var enemy = new SerializedObject(root.GetComponent<RangedEnemy>());
            enemy.FindProperty("_turretPivot").objectReferenceValue = pivot;
            enemy.FindProperty("_firePoint").objectReferenceValue = muzzle;
            enemy.FindProperty("_bulletPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ShellPath).GetComponent<EnemyBullet>();
            enemy.FindProperty("_eyeHeight").floatValue = pivot.localPosition.y;
            enemy.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, EnemyPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CreateShell()
    {
        var root = PrefabUtility.LoadPrefabContents(Folder + "Mortar/MortarShell.prefab");
        try
        {
            root.name = "CannonShell";
            root.transform.localScale = Vector3.one;
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshFilter>());
            var model = AddModel("ball_mdl", root.transform, 1f);
            float diameter = BoundsOf(model).size.x;
            model.transform.localScale *= 0.22f / diameter;
            PrefabUtility.SaveAsPrefabAsset(root, ShellPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static GameObject AddModel(string name, Transform parent, float scale)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + name + ".fbx");
        if (source == null) throw new InvalidOperationException("Missing Cannon model: " + name);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        // Preserve the FBX's authored offsets and unit conversion for all parts.
        model.transform.localPosition = source.transform.localPosition * scale;
        model.transform.localRotation = source.transform.localRotation;
        model.transform.localScale = source.transform.localScale * scale;
        var material = AssetDatabase.LoadAssetAtPath<Material>(Models + "Materials/cyanon_UV_1001.mat");
        if (material == null) throw new InvalidOperationException("Missing Cannon material");
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
        return model;
    }

    private static Bounds BoundsOf(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new InvalidOperationException("Cannon model has no renderer");
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void AddHitBox(GameObject model)
    {
        var bounds = BoundsOf(model);
        var collider = model.AddComponent<BoxCollider>();
        collider.center = model.transform.InverseTransformPoint(bounds.center);
        collider.size = model.transform.InverseTransformVector(bounds.size);
    }
}
