using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CannonEnemyValidation
{
    private const string Folder = "Assets/Resources/Prefab/Enemy/";

    // Run with -executeMethod CannonEnemyValidation.Validate in batch mode.
    [MenuItem("DECO-Min/Validate Cannon Enemy")]
    public static void Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "CannonEnemy.prefab");
        Require(prefab != null, "CannonEnemy prefab is missing");
        var enemy = prefab.GetComponent<RangedEnemy>();
        Require(enemy != null && enemy.IsStationary, "Cannon must use stationary ranged combat");
        var data = new SerializedObject(enemy);
        var pivot = data.FindProperty("_turretPivot").objectReferenceValue as Transform;
        var muzzle = data.FindProperty("_firePoint").objectReferenceValue as Transform;
        var bullet = data.FindProperty("_bulletPrefab").objectReferenceValue as EnemyBullet;
        Require(pivot != null && muzzle != null && muzzle.IsChildOf(pivot), "Muzzle must follow turret rotation");
        Require(bullet != null && AssetDatabase.GetAssetPath(bullet) == Folder + "CannonShell.prefab", "Cannon shell is not connected");
        Require(data.FindProperty("characterStatus").objectReferenceValue != null, "Enemy status is missing");
        Require(prefab.GetComponentsInChildren<Collider>().Length >= 2, "Base and head need hit colliders");
        Require(prefab.GetComponent<Rigidbody>().constraints == RigidbodyConstraints.FreezeAll, "Base must remain fixed");
        Require(prefab.GetComponentsInChildren<Renderer>().Length >= 2, "Cannon model parts are missing");
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
            Require(filter.sharedMesh != null && AssetDatabase.GetAssetPath(filter.sharedMesh).Contains("cannon_mdl/"), "Cannon must use supplied model meshes");
        Require(bullet.GetComponentInChildren<MeshFilter>() != null, "Shell model is missing");
        Require(bullet.TryGetAreaLaunchVelocity(muzzle.position, new Vector3(0, 0, 10), out var velocity), "Shell cannot reach attack range");
        float time = (10 - muzzle.position.z) / velocity.z;
        float gravity = new SerializedObject(bullet).FindProperty("_gravity").floatValue;
        var landing = muzzle.position + velocity * time + Vector3.down * (0.5f * gravity * time * time);
        Require(Vector3.Distance(landing, new Vector3(0, 0, 10)) < 0.01f, "Shell trajectory misses target");
        ValidateFiring();
        Debug.Log("CANNON VALIDATION PASSED: model, combat references, colliders, ballistic range, turret firing and fixed base.");
    }

    private static void ValidateFiring()
    {
        // Exercise the actual preparation/firing methods on an isolated prefab copy.
        // Ignore open-scene geometry so this check works in any authoring scene.
        var existing = new HashSet<EnemyBullet>(Resources.FindObjectsOfTypeAll<EnemyBullet>());
        var root = PrefabUtility.LoadPrefabContents(Folder + "CannonEnemy.prefab");
        try
        {
            var target = new GameObject("ValidationTarget").transform;
            target.SetParent(root.transform, false);
            target.position = new Vector3(3, 0, 8);
            var enemy = root.GetComponent<RangedEnemy>();
            var data = new SerializedObject(enemy);
            data.FindProperty("_target").objectReferenceValue = target;
            data.FindProperty("_visionBlockMask").intValue = 0;
            data.FindProperty("_landingSurfaceMask").intValue = 0;
            data.ApplyModifiedPropertiesWithoutUndo();
            var pivot = (Transform)data.FindProperty("_turretPivot").objectReferenceValue;
            var body = root.transform.Find("CannonBase");
            var basePosition = body.position;
            var baseRotation = body.rotation;
            Require(enemy.BeginPreparation(), "Cannon could not begin preparing a shot");
            for (int i = 0; i < 30 && enemy.IsPreparing; i++) enemy.TickPreparation(0.1f);
            Require(!enemy.CanBeginPreparation, "Cannon failed to fire and enter cooldown");
            Require(Quaternion.Angle(pivot.rotation, Quaternion.identity) > 1, "Head did not aim at off-axis target");
            Require(body.position == basePosition && body.rotation == baseRotation, "Aiming moved the fixed base");
            int fired = 0;
            foreach (var shot in Resources.FindObjectsOfTypeAll<EnemyBullet>())
                if (!existing.Contains(shot) && !EditorUtility.IsPersistent(shot)) fired++;
            Require(fired == 1, "Preparation should create exactly one projectile");
        }
        finally
        {
            foreach (var shot in Resources.FindObjectsOfTypeAll<EnemyBullet>())
                if (!existing.Contains(shot) && !EditorUtility.IsPersistent(shot))
                    UnityEngine.Object.DestroyImmediate(shot.gameObject);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("DECO-Min/Inspect Cannon Models")]
    public static void InspectModels()
    {
        foreach (var name in new[] { "nannon_body_mdl", "cannon_head_mdl", "ball_mdl" })
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Model/cannon_mdl/" + name + ".fbx");
            var instance = UnityEngine.Object.Instantiate(source);
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
                Debug.Log($"CANNON MODEL {name}: transform={filter.transform.position:F4}, rotation={filter.transform.eulerAngles}, scale={filter.transform.lossyScale:F4}, mesh={filter.sharedMesh.bounds}, world={filter.GetComponent<Renderer>().bounds}");
            UnityEngine.Object.DestroyImmediate(instance);
        }
        Validate();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
