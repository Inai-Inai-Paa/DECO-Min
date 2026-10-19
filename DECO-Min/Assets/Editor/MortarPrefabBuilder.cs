using System.IO;
using UnityEditor;
using UnityEngine;

public static class MortarPrefabBuilder
{
    private const string Folder = "Assets/Resources/Prefab/Enemy/Mortar";

    [MenuItem("DECO-Min/Create Mortar Prefabs")]
    public static void Create()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/MortarTurret.prefab") != null) return;
        Material yellow = Material("MortarYellow", new Color(1f, 0.72f, 0.06f));
        Material dark = Material("MortarDark", new Color(0.18f, 0.2f, 0.23f));
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "MortarShell";
        shell.transform.localScale = Vector3.one * 0.22f;
        Object.DestroyImmediate(shell.GetComponent<Collider>());
        shell.GetComponent<Renderer>().sharedMaterial = dark;
        shell.AddComponent<EnemyBullet>();
        GameObject shellPrefab = PrefabUtility.SaveAsPrefabAsset(shell, Folder + "/MortarShell.prefab");
        Object.DestroyImmediate(shell);

        GameObject root = new GameObject("MortarTurret");
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;
        body.centerOfMass = Vector3.zero;
        RangedEnemy enemy = root.AddComponent<RangedEnemy>();
        root.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        Part("Body1", root.transform, new Vector3(0, 0.18f, 0), new Vector3(0.95f, 0.36f, 0.9f), yellow);
        Part("LeftFoot", root.transform, new Vector3(-0.4f, 0.07f, 0), new Vector3(0.25f, 0.14f, 1.1f), dark);
        Part("RightFoot", root.transform, new Vector3(0.4f, 0.07f, 0), new Vector3(0.25f, 0.14f, 1.1f), dark);
        Transform pivot = new GameObject("TurretPivot").transform;
        pivot.SetParent(root.transform, false);
        pivot.localPosition = new Vector3(0, 0.65f, 0);
        Part("Body2", pivot, Vector3.zero, new Vector3(0.65f, 0.5f, 0.65f), yellow);
        Part("Barrel", pivot, new Vector3(0, 0, 0.5f), new Vector3(0.24f, 0.24f, 0.55f), dark);
        Transform muzzle = new GameObject("FirePoint").transform;
        muzzle.SetParent(pivot, false);
        muzzle.localPosition = new Vector3(0, 0, 0.8f);
        var settings = new SerializedObject(enemy);
        settings.FindProperty("_turretPivot").objectReferenceValue = pivot;
        settings.FindProperty("_firePoint").objectReferenceValue = muzzle;
        settings.FindProperty("_bulletPrefab").objectReferenceValue = shellPrefab.GetComponent<EnemyBullet>();
        settings.FindProperty("_eyeHeight").floatValue = 0.8f;
        settings.FindProperty("_attackDamage").intValue = 10;
        var referenceEnemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefab/Enemy/testEnemy.prefab");
        if (referenceEnemy != null && referenceEnemy.TryGetComponent<Enemy>(out var reference))
            settings.FindProperty("_sealPrefab").objectReferenceValue =
                new SerializedObject(reference).FindProperty("_sealPrefab").objectReferenceValue;
        settings.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/MortarTurret.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Mortar prefabs created: " + Folder);
    }

    private static Material Material(string name, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
}
