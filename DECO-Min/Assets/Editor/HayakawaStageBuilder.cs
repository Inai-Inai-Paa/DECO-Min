using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class HayakawaStageBuilder
{
    private const string ScenePath = "Assets/Scenes/HayakawaTestScene.unity";
    private const string RequestFileName = "build-hayakawa-stage.txt";

    private const float Floor = 0f;
    private const float Gap = 1.5f;

    [MenuItem("DECO-Min/Build Hayakawa Stage")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        EditorSceneManager.SetActiveScene(scene);

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            UnityEngine.Object.DestroyImmediate(roots[i]);
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.48f, 0.44f, 0.38f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.15f, 0.13f);
        RenderSettings.fog = false;

        Material pit = CreateMaterial(new Color(0.13f, 0.14f, 0.17f));
        Material path = CreateMaterial(new Color(0.84f, 0.75f, 0.58f));
        Material island = CreateMaterial(new Color(0.73f, 0.82f, 0.76f));
        Material plaza = CreateMaterial(new Color(0.72f, 0.50f, 0.38f));
        Material goal = CreateMaterial(new Color(0.58f, 0.74f, 0.46f));
        Material wall = CreateMaterial(new Color(0.28f, 0.24f, 0.20f));
        Material cover = CreateMaterial(new Color(0.40f, 0.44f, 0.48f));
        Material stone = CreateMaterial(new Color(0.24f, 0.26f, 0.29f));
        Material ramp = CreateMaterial(new Color(0.46f, 0.40f, 0.32f));
        Material accent = CreateMaterial(new Color(0.95f, 0.78f, 0.28f));

        GameObject stage = new GameObject("Stage 01 Garden Crossing");
        Transform geometry = new GameObject("Geometry").transform;
        geometry.SetParent(stage.transform, false);

        CreateBox(geometry, "SafetyFloor", new Vector3(0f, -4.5f, 36f), new Vector3(56f, 1f, 110f), pit);

        const float startCenterZ = -2f;
        const float startLength = 16f;
        float startEndZ = startCenterZ + startLength * 0.5f;
        Platform(geometry, "StartCourt", 0f, startCenterZ, 18f, startLength, path);
        CreateBox(geometry, "SpawnMark", new Vector3(0f, 0.04f, -6f), new Vector3(2.2f, 0.08f, 2.2f), accent, false);

        Wall(geometry, "StartBackWall", 0f, -9.7f, 18f, 0.6f, wall);
        Wall(geometry, "StartRightWall", 8.7f, startCenterZ, 0.6f, startLength, wall);
        Wall(geometry, "StartLeftWallBack", -8.7f, -8f, 0.6f, 4f, wall);
        Wall(geometry, "StartLeftWallFront", -8.7f, 2.5f, 0.6f, 7f, wall);

        float island1Start = startEndZ + Gap;
        float island1Center = island1Start + 3.5f;
        float island1End = island1Start + 7f;
        Platform(geometry, "JumpIsland_01", -1.5f, island1Center, 7f, 7f, island);

        float island2Start = island1End + Gap;
        float island2Center = island2Start + 3.5f;
        float island2End = island2Start + 7f;
        Platform(geometry, "JumpIsland_02", 1.5f, island2Center, 7f, 7f, island);

        float island3Start = island2End + Gap;
        float island3Center = island3Start + 3.5f;
        float island3End = island3Start + 7f;
        Platform(geometry, "JumpIsland_03", 0f, island3Center, 7f, 7f, island);

        float plazaStart = island3End + Gap;
        const float plazaLength = 20f;
        float plazaCenter = plazaStart + plazaLength * 0.5f;
        float plazaEnd = plazaStart + plazaLength;
        Platform(geometry, "CombatPlaza", 0f, plazaCenter, 22f, plazaLength, plaza);
        Wall(geometry, "PlazaLeftWall", -10.7f, plazaCenter, 0.6f, plazaLength, wall);
        Wall(geometry, "PlazaRightWall", 10.7f, plazaCenter, 0.6f, plazaLength, wall);
        Wall(geometry, "PlazaBackLeft", -6.5f, plazaEnd - 0.3f, 9f, 0.6f, wall);
        Wall(geometry, "PlazaBackRight", 6.5f, plazaEnd - 0.3f, 9f, 0.6f, wall);

        CreateBox(geometry, "Cover_A", new Vector3(-5f, 0.9f, plazaCenter - 2f), new Vector3(2.4f, 1.8f, 2.2f), cover);
        CreateBox(geometry, "Cover_B", new Vector3(4.5f, 0.9f, plazaCenter + 3f), new Vector3(2.2f, 1.8f, 2.6f), cover);
        CreateBox(geometry, "Cover_C", new Vector3(0.5f, 0.8f, plazaStart + 4f), new Vector3(3.2f, 1.6f, 1.4f), cover);

        const float pedestalTop = 1.8f;
        CreateBox(
            geometry,
            "MortarPedestal",
            new Vector3(0f, pedestalTop * 0.5f, plazaEnd - 3f),
            new Vector3(4.5f, pedestalTop, 3.6f),
            stone);

        float bridgeStart = plazaEnd + Gap;
        const float bridgeLength = 8f;
        float bridgeCenter = bridgeStart + bridgeLength * 0.5f;
        float bridgeEnd = bridgeStart + bridgeLength;
        Platform(geometry, "Bridge", 0f, bridgeCenter, 3.5f, bridgeLength, path);

        float goalStart = bridgeEnd + Gap;
        const float goalLength = 12f;
        float goalCenter = goalStart + goalLength * 0.5f;
        float goalEnd = goalStart + goalLength;
        Platform(geometry, "GoalCourt", 0f, goalCenter, 14f, goalLength, goal);
        Wall(geometry, "GoalBackWall", 0f, goalEnd - 0.3f, 14f, 0.5f, wall);
        Wall(geometry, "GoalLeftWall", -6.7f, goalCenter, 0.5f, goalLength, wall);
        Wall(geometry, "GoalRightWall", 6.7f, goalCenter, 0.5f, goalLength, wall);
        CreateBox(geometry, "GoalPillar_L", new Vector3(-3.2f, 1.1f, goalCenter + 2.5f), new Vector3(0.7f, 2.2f, 0.7f), accent);
        CreateBox(geometry, "GoalPillar_R", new Vector3(3.2f, 1.1f, goalCenter + 2.5f), new Vector3(0.7f, 2.2f, 0.7f), accent);

        CreateRamp(
            geometry,
            "ReturnRamp",
            new Vector3(-16f, -4f, -4f),
            new Vector3(-8.2f, 0.05f, -4f),
            5f,
            0.4f,
            ramp);

        GameObject navObject = new GameObject("NavMesh");
        navObject.transform.SetParent(stage.transform, false);
        NavMeshSurface surface = navObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = 1;
        surface.BuildNavMesh();
        const string navMeshAssetPath = "Assets/Scenes/Hayakawa/NavMesh-Stage.asset";
        if (surface.navMeshData != null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/Hayakawa"))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "Hayakawa");
            }

            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(navMeshAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(navMeshAssetPath);
            }

            AssetDatabase.CreateAsset(surface.navMeshData, navMeshAssetPath);
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Scripts/GameObject/Character/Player/Player.prefab");
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.SetParent(stage.transform, false);
        player.transform.position = new Vector3(0f, 1.1f, -6f);
        player.transform.rotation = Quaternion.identity;
        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = player.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Resources/Animation/Player/Animator/Player.controller");
        SerializedObject playerData = new SerializedObject(player.GetComponent<Player>());
        playerData.FindProperty("_animator").objectReferenceValue = animator;
        playerData.ApplyModifiedPropertiesWithoutUndo();

        Transform enemies = new GameObject("Enemies").transform;
        enemies.SetParent(stage.transform, false);
        EnemyStatus enemyStatus = AssetDatabase.LoadAssetAtPath<EnemyStatus>(
            "Assets/Scripts/GameObject/Character/Enemy/Status/DefaultEnemyStatus.asset");
        GameObject tacklePrefab = LoadFirstPrefab(
            "Assets/Resources/Prefab/Enemy/TackleEnemy.prefab",
            "Assets/Resources/Prefab/Enemy/testEnemy.prefab",
            "Assets/Resources/Prefab/Enemy/testEnemy 1.prefab");
        PlaceTackleEncounter(tacklePrefab, enemies, enemyStatus, plazaCenter);

        GameObject mortarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Prefab/Enemy/CannonEnemy.prefab");
        PlaceEnemy(
            mortarPrefab,
            enemies,
            "CannonEnemy",
            new Vector3(0f, pedestalTop, plazaEnd - 3f),
            Quaternion.Euler(0f, 180f, 0f),
            enemyStatus);

        GameObject sealPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Prefab/Seal/Seal.prefab");
        GameObject seal = (GameObject)PrefabUtility.InstantiatePrefab(sealPrefab);
        seal.name = "GoalSeal";
        seal.transform.SetParent(stage.transform, false);
        seal.transform.position = new Vector3(0f, 0.12f, goalCenter + 1.5f);
        seal.transform.localScale = new Vector3(1.6f, 0.12f, 1.6f);

        CameraState followState = AssetDatabase.LoadAssetAtPath<CameraState>(
            "Assets/Scripts/GameObject/Camera/Controller/State/Follow/TPS.asset");
        Transform cameras = new GameObject("Camera Regions").transform;
        cameras.SetParent(stage.transform, false);
        CreateVolume(cameras, "Fallback", new Vector3(0f, 1f, 36f), 95f, -10, new Vector3(0f, 8f, -12f), 55f, followState);
        CreateVolume(cameras, "Start", new Vector3(0f, 1f, startCenterZ), 16f, 0, new Vector3(0f, 6.5f, -11f), 52f, followState);
        CreateVolume(cameras, "Islands", new Vector3(0f, 1f, island2Center), 18f, 1, new Vector3(0f, 9f, -13f), 58f, followState);
        CreateVolume(cameras, "Combat", new Vector3(0f, 2f, plazaCenter), 12f, 2, new Vector3(0f, 12f, -15f), 60f, followState);
        CreateVolume(cameras, "Goal", new Vector3(0f, 1f, goalCenter), 14f, 3, new Vector3(0f, 5f, -8f), 46f, followState);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.position = new Vector3(0f, 8f, -18f);
        cameraObject.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        CameraController controller = cameraObject.AddComponent<CameraController>();
        SerializedObject controllerData = new SerializedObject(controller);
        controllerData.FindProperty("_player").objectReferenceValue = player.GetComponent<Player>();
        controllerData.FindProperty("_controlledCamera").objectReferenceValue = camera;
        controllerData.FindProperty("_defaultStateTemplate").objectReferenceValue = followState;
        controllerData.ApplyModifiedPropertiesWithoutUndo();

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;

        GameObject volumeObject = new GameObject("Global Volume");
        volumeObject.transform.SetParent(stage.transform, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Settings/DefaultVolumeProfile.asset");

        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Built stage 01 Garden Crossing in HayakawaTestScene.");
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
            Build();
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
                Build();
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

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        material.color = color;
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.12f);
        }

        return material;
    }

    private static GameObject Platform(
        Transform parent,
        string name,
        float centerX,
        float centerZ,
        float sizeX,
        float sizeZ,
        Material material)
    {
        const float thickness = 1f;
        return CreateBox(
            parent,
            name,
            new Vector3(centerX, Floor - thickness * 0.5f, centerZ),
            new Vector3(sizeX, thickness, sizeZ),
            material);
    }

    private static GameObject Wall(
        Transform parent,
        string name,
        float centerX,
        float centerZ,
        float sizeX,
        float sizeZ,
        Material material)
    {
        const float height = 2.2f;
        return CreateBox(
            parent,
            name,
            new Vector3(centerX, Floor + height * 0.5f, centerZ),
            new Vector3(sizeX, height, sizeZ),
            material);
    }

    private static GameObject CreateBox(
        Transform parent,
        string name,
        Vector3 center,
        Vector3 scale,
        Material material,
        bool collider = true)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = center;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
        {
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        return box;
    }

    private static void CreateRamp(
        Transform parent,
        string name,
        Vector3 surfaceStart,
        Vector3 surfaceEnd,
        float width,
        float thickness,
        Material material)
    {
        Vector3 delta = surfaceEnd - surfaceStart;
        Vector3 forward = delta.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        right.Normalize();
        Vector3 normal = Vector3.Cross(forward, right).normalized;

        GameObject rampObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampObject.name = name;
        rampObject.transform.SetParent(parent, false);
        rampObject.transform.rotation = Quaternion.LookRotation(forward, normal);
        rampObject.transform.position = (surfaceStart + surfaceEnd) * 0.5f - normal * (thickness * 0.5f);
        rampObject.transform.localScale = new Vector3(width, thickness, delta.magnitude);
        rampObject.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static GameObject LoadFirstPrefab(params string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);

            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private static void PlaceTackleEncounter(
        GameObject prefab,
        Transform parent,
        EnemyStatus status,
        float plazaCenter)
    {
        if (prefab == null)
        {
            Debug.LogError("Tackle enemy prefab was not found. Combat plaza enemies were not placed.");
            return;
        }

        Quaternion faceEntrance = Quaternion.Euler(0f, 180f, 0f);
        GameObject tackleA = PlaceEnemy(
            prefab,
            parent,
            "Tackle_A",
            new Vector3(-6f, 0.5f, plazaCenter + 1f),
            faceEntrance,
            status);
        GameObject tackleB = PlaceEnemy(
            prefab,
            parent,
            "Tackle_B",
            new Vector3(6f, 0.5f, plazaCenter - 1f),
            faceEntrance,
            status);

        GameObject territory = new GameObject("TackleTerritory");
        territory.transform.SetParent(parent, false);
        territory.transform.position = new Vector3(0f, 0f, plazaCenter);
        Vector3 territorySize = new Vector3(20f, 8f, 18f);
        ConfigureTackleTerritory(tackleA, territory.transform, territorySize);
        ConfigureTackleTerritory(tackleB, territory.transform, territorySize);

        GameObject areaObject = new GameObject("TackleSpawnArea");
        areaObject.transform.SetParent(parent, false);
        areaObject.transform.position = new Vector3(0f, 1f, plazaCenter);
        EnemySpawnArea area = areaObject.AddComponent<EnemySpawnArea>();
        SerializedObject areaData = new SerializedObject(area);
        areaData.FindProperty("_spawnRadius").floatValue = 14f;
        areaData.FindProperty("_leashRadius").floatValue = 18f;
        areaData.FindProperty("_despawnDelay").floatValue = 6f;
        areaData.FindProperty("_useIndividualSpawnPoses").boolValue = true;
        SerializedProperty enemyList = areaData.FindProperty("_enemies");
        enemyList.arraySize = 2;
        enemyList.GetArrayElementAtIndex(0).objectReferenceValue = tackleA.GetComponent<Enemy>();
        enemyList.GetArrayElementAtIndex(1).objectReferenceValue = tackleB.GetComponent<Enemy>();
        areaData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureTackleTerritory(GameObject enemy, Transform center, Vector3 size)
    {
        if (enemy == null)
        {
            return;
        }

        Enemy enemyComponent = enemy.GetComponent<Enemy>();

        if (enemyComponent == null)
        {
            return;
        }

        SerializedObject data = new SerializedObject(enemyComponent);
        data.FindProperty("_useMoveArea").boolValue = true;
        data.FindProperty("_moveAreaCenter").objectReferenceValue = center;
        data.FindProperty("_moveAreaSize").vector3Value = size;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject PlaceEnemy(
        GameObject prefab,
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        EnemyStatus status)
    {
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        enemy.name = name;
        enemy.transform.SetParent(parent, false);
        enemy.transform.SetPositionAndRotation(position, rotation);

        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent == null || status == null)
        {
            return enemy;
        }

        SerializedObject data = new SerializedObject(enemyComponent);
        data.FindProperty("characterStatus").objectReferenceValue = status;
        data.ApplyModifiedPropertiesWithoutUndo();
        return enemy;
    }

    private static void CreateVolume(
        Transform parent,
        string name,
        Vector3 position,
        float radius,
        int priority,
        Vector3 cameraOffset,
        float fieldOfView,
        CameraState stateTemplate)
    {
        GameObject volumeObject = new GameObject(name);
        volumeObject.transform.SetParent(parent, false);
        volumeObject.transform.position = position;
        CameraVolume volume = volumeObject.AddComponent<CameraVolume>();
        SerializedObject data = new SerializedObject(volume);
        data.FindProperty("_radius").floatValue = radius;
        data.FindProperty("_priority").intValue = priority;
        data.FindProperty("_stateTemplate").objectReferenceValue = stateTemplate;
        SerializedProperty config = data.FindProperty("_config");
        config.FindPropertyRelative("preventTerrainPenetration").boolValue = false;
        config.FindPropertyRelative("targetOffset").vector3Value = new Vector3(0f, 1.5f, 0f);
        config.FindPropertyRelative("cameraOffset").vector3Value = cameraOffset;
        config.FindPropertyRelative("fieldOfView").floatValue = fieldOfView;
        config.FindPropertyRelative("smoothnessPosition").floatValue = 5f;
        config.FindPropertyRelative("smoothnessTarget").floatValue = 20f;
        config.FindPropertyRelative("enterSmoothnessPosition").floatValue = 1.5f;
        config.FindPropertyRelative("enterSmoothnessTarget").floatValue = 2f;
        config.FindPropertyRelative("enterSmoothnessRecoveryDuration").floatValue = 3f;
        config.FindPropertyRelative("rotationLimitMin").vector3Value = new Vector3(-10f, -30f, 0f);
        config.FindPropertyRelative("rotationLimitMax").vector3Value = new Vector3(40f, 30f, 0f);
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
