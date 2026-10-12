#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

public static class BlueBoxLevelBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/BlueBoxLab.unity";
    private const string ProfilePath = "Assets/_Project/Settings/Volumes/BlueBoxMood.asset";
    private const string AmbientMusicPath = "Assets/_Project/Audio/BlueBox_Ambient.wav";

    [MenuItem("Blue Box/Build PA4 Game")]
    public static void Build()
    {
        EnsureFolders();
        EnsureAmbientMusicAsset();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Transform environment = Group("ENVIRONMENT");
        Transform architecture = Group("Architecture", environment);
        Transform props = Group("Props", environment);
        Transform lighting = Group("Lighting");
        Transform effects = Group("Effects");

        Material floor = MakeMaterial("M_Floor", new Color(0.07f, 0.11f, 0.15f), 0.35f, 0.35f);
        Material wall = MakeMaterial("M_Wall", new Color(0.18f, 0.24f, 0.29f), 0.25f, 0.15f);
        Material trim = MakeMaterial("M_Trim", new Color(0.04f, 0.65f, 0.78f), 0.7f, 0.25f, true);
        Material metal = MakeMaterial("M_Metal", new Color(0.32f, 0.39f, 0.43f), 0.8f, 0.45f);
        Material amber = MakeMaterial("M_Amber", new Color(0.9f, 0.36f, 0.08f), 0.25f, 0.1f, true);
        Material glass = MakeMaterial("M_Reactor", new Color(0.06f, 0.58f, 0.78f), 0.3f, 0.2f, true);
        Material pulseMaterial = MakeReactorPulseMaterial(glass);
        Material hazard = MakeMaterial("M_Hazard", new Color(0.95f, 0.08f, 0.025f), 0.2f, 0.25f, true);
        Material spill = MakeMaterial("M_ChemicalSpill", new Color(0.04f, 0.5f, 0.34f), 0.1f, 0.2f, true);

        // A wide, flat route is divided into three clean sections with a clear center line.
        PbCube("LowerFloor", architecture, new Vector3(0f, -0.25f, 31f), new Vector3(16f, 0.5f, 64f), floor);
        PbCube("WestWall", architecture, new Vector3(-8f, 2.8f, 31f), new Vector3(0.5f, 6f, 64f), wall);
        PbCube("EastWall", architecture, new Vector3(8f, 2.8f, 31f), new Vector3(0.5f, 6f, 64f), wall);
        // Seal the open end behind the player start so reversing cannot leave the playable level.
        PbCube("EntryBackWall", architecture, new Vector3(0f, 2.8f, -0.75f), new Vector3(16.5f, 6f, 0.5f), wall);
        PbCube("BackLeftWall", architecture, new Vector3(-5.4f, 2.8f, 63f), new Vector3(5.2f, 6f, 0.5f), wall);
        PbCube("BackRightWall", architecture, new Vector3(5.4f, 2.8f, 63f), new Vector3(5.2f, 6f, 0.5f), wall);
        PbCube("BackLintel", architecture, new Vector3(0f, 5.4f, 63f), new Vector3(1.6f, 0.8f, 0.5f), wall);
        PbCube("RouteLight_Left", architecture, new Vector3(-7.62f, 0.03f, 31f), new Vector3(0.08f, 0.08f, 58f), trim);
        PbCube("RouteLight_Right", architecture, new Vector3(7.62f, 0.03f, 31f), new Vector3(0.08f, 0.08f, 58f), trim);
        for (int section = 0; section < 3; section++)
        {
            float z = 10f + section * 20f;
            PbCube("SectionMarker_" + (section + 1), architecture, new Vector3(0f, 5.35f, z), new Vector3(15f, 0.12f, 0.12f), trim);
        }

        // Wall-side consoles keep the middle lane open and readable.
        MakeConsole(props, new Vector3(-6.45f, 0.65f, 12f), metal, trim);
        MakeConsole(props, new Vector3(-6.45f, 0.65f, 24f), metal, amber);
        MakeConsole(props, new Vector3(-6.45f, 0.65f, 43f), metal, amber);
        MakeConsole(props, new Vector3(6.45f, 0.65f, 55f), metal, trim);

        // An accessible raised service catwalk adds a second route through the lab.
        for (int step = 0; step < 3; step++)
            PbCube("CatwalkStair_Up_" + (step + 1), architecture,
                new Vector3(5.25f, 0.2f + step * 0.4f, 14.2f + step * 1.25f),
                new Vector3(2.1f, 0.4f, 2.5f), metal);
        PbCube("RaisedServiceCatwalk", architecture, new Vector3(5.25f, 1.4f, 21.2f),
            new Vector3(3.2f, 0.3f, 8.2f), floor);
        PbCube("CatwalkEdgeLight_Left", architecture, new Vector3(3.72f, 1.58f, 21.2f),
            new Vector3(0.06f, 0.06f, 7.8f), trim);
        PbCube("CatwalkEdgeLight_Right", architecture, new Vector3(6.78f, 1.58f, 21.2f),
            new Vector3(0.06f, 0.06f, 7.8f), trim);
        for (int step = 0; step < 3; step++)
            PbCube("CatwalkStair_Down_" + (step + 1), architecture,
                new Vector3(5.25f, 1.0f - step * 0.4f, 25.3f + step * 1.25f),
                new Vector3(2.1f, 0.4f, 2.5f), metal);

        // Silent environmental evidence of a containment breach: spill, fallen canister and warning lights.
        MakeCanister(props, new Vector3(-5.5f, 0.62f, 20.3f), amber, hazard);
        GameObject fallenCanister = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fallenCanister.name = "FallenChemicalCanister";
        fallenCanister.transform.SetParent(props, false);
        fallenCanister.transform.SetPositionAndRotation(new Vector3(-4.2f, 0.38f, 21f), Quaternion.Euler(0f, 0f, 82f));
        fallenCanister.transform.localScale = new Vector3(0.36f, 0.55f, 0.36f);
        fallenCanister.GetComponent<Renderer>().sharedMaterial = amber;
        Object.DestroyImmediate(fallenCanister.GetComponent<Collider>());
        GameObject spillPool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spillPool.name = "ChemicalSpill";
        spillPool.transform.SetParent(props, false);
        spillPool.transform.SetPositionAndRotation(new Vector3(-4.7f, 0.015f, 20.8f), Quaternion.identity);
        spillPool.transform.localScale = new Vector3(1.45f, 0.015f, 0.9f);
        spillPool.GetComponent<Renderer>().sharedMaterial = spill;
        Object.DestroyImmediate(spillPool.GetComponent<Collider>());

        // Seven visible energy cells sit just above the obstacles and require a jump to collect.
        CoinManager coinManager = new GameObject("EnergyCellManager").AddComponent<CoinManager>();
        Transform pickups = Group("EnergyCells", props);
        Material coinMaterial = MakeMaterial("M_EnergyCell", new Color(1f, 0.72f, 0.08f), 0.75f, 0.2f, true);
        Vector3[] coinPositions = {
            new Vector3(0f, 3.15f, 3.7f), new Vector3(3.2f, 3.15f, 11.7f),
            new Vector3(-3.2f, 3.15f, 19.7f), new Vector3(3.2f, 3.15f, 27.7f),
            new Vector3(-3.2f, 3.15f, 35.7f), new Vector3(3.2f, 3.15f, 43.7f),
            new Vector3(0f, 3.15f, 51.7f)
        };
        foreach (Vector3 coinPosition in coinPositions)
            MakeEnergyCell(pickups, coinPosition, coinMaterial, coinManager);

        // Each energy cell hovers well above a small touch hazard and can only be collected during a jump.
        foreach (Vector3 coinPosition in coinPositions)
            MakeHazardBlock(props, new Vector3(coinPosition.x, 0.325f, coinPosition.z),
                new Vector3(1.3f, 0.65f, 1.3f), metal, hazard, amber);

        // A checkpoint arch spans the path just beyond the first half of the course.
        Transform checkpointAnchor = new GameObject("CheckpointRespawnPoint").transform;
        checkpointAnchor.SetParent(props, false);
        checkpointAnchor.SetPositionAndRotation(new Vector3(0f, 0.1f, 33f), Quaternion.identity);
        PbCube("CheckpointPillar_Left", props, new Vector3(-2.5f, 1.6f, 32f), new Vector3(0.28f, 3.2f, 0.28f), trim);
        PbCube("CheckpointPillar_Right", props, new Vector3(2.5f, 1.6f, 32f), new Vector3(0.28f, 3.2f, 0.28f), trim);
        PbCube("CheckpointBeam", props, new Vector3(0f, 3.2f, 32f), new Vector3(5.28f, 0.28f, 0.28f), trim);
        GameObject checkpoint = new GameObject("CheckpointTrigger");
        checkpoint.transform.position = new Vector3(0f, 1.5f, 32f);
        BoxCollider checkpointCollider = checkpoint.AddComponent<BoxCollider>();
        checkpointCollider.isTrigger = true;
        checkpointCollider.size = new Vector3(5f, 3f, 1.6f);
        CheckpointTrigger checkpointTrigger = checkpoint.AddComponent<CheckpointTrigger>();
        SerializedObject checkpointSerialized = new SerializedObject(checkpointTrigger);
        checkpointSerialized.FindProperty("respawnPoint").objectReferenceValue = checkpointAnchor;
        checkpointSerialized.FindProperty("indicatorLight").objectReferenceValue = MakePointLight("CheckpointLight", lighting, new Vector3(0f, 3.8f, 32f), new Color(1f, 0.48f, 0.06f), 1.5f, 7f, LightmapBakeType.Realtime);
        checkpointSerialized.ApplyModifiedPropertiesWithoutUndo();

        // Reactor, animated door, activation light, and a visible steam effect.
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "ReactorCore";
        core.transform.SetParent(props, false);
        core.transform.SetPositionAndRotation(new Vector3(0f, 1.4f, 59f), Quaternion.identity);
        core.transform.localScale = new Vector3(1.3f, 1.4f, 1.3f);
        core.GetComponent<Renderer>().sharedMaterial = pulseMaterial != null ? pulseMaterial : glass;

        GameObject doorPivot = new GameObject("ExitDoorPivot");
        doorPivot.transform.SetParent(architecture, false);
        doorPivot.transform.localPosition = new Vector3(-2.8f, 2.5f, 62.7f);
        GameObject doorLeaf = PbCube("ExitDoor", doorPivot.transform, new Vector3(2.8f, 0f, 0f), new Vector3(5.6f, 5f, 0.3f), amber);
        Collider doorCollider = doorLeaf.GetComponent<Collider>();
        if (doorCollider != null) doorCollider.isTrigger = false;

        Light reactorLight = MakePointLight("ReactorLight", lighting, new Vector3(0f, 3.8f, 59f), new Color(0.1f, 0.7f, 1f), 2.5f, 11f, LightmapBakeType.Realtime);
        Light[] activatedLights = { reactorLight };
        GameObject steam = MakeSteam(effects, new Vector3(-6.6f, 2.1f, 20f));

        ReactorCore reactor = core.AddComponent<ReactorCore>();
        SerializedObject reactorSerialized = new SerializedObject(reactor);
        reactorSerialized.FindProperty("sealedDoor").objectReferenceValue = doorPivot.transform;
        reactorSerialized.FindProperty("coinManager").objectReferenceValue = coinManager;
        reactorSerialized.FindProperty("lightsToActivate").arraySize = activatedLights.Length;
        reactorSerialized.FindProperty("lightsToActivate").GetArrayElementAtIndex(0).objectReferenceValue = reactorLight;
        reactorSerialized.FindProperty("activationEffect").objectReferenceValue = steam.GetComponent<ParticleSystem>();
        reactorSerialized.ApplyModifiedPropertiesWithoutUndo();
        reactorLight.enabled = false;
        steam.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Mixed approach: one baked key light for static architecture plus local realtime lights.
        GameObject keyObject = new GameObject("BakedKeyLight");
        keyObject.transform.SetParent(lighting, false);
        keyObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(0.68f, 0.8f, 1f);
        key.intensity = 0.7f;
        key.lightmapBakeType = LightmapBakeType.Baked;
        key.shadows = LightShadows.Soft;

        MakePointLight("EntryEmergencyLight", lighting, new Vector3(0f, 3.8f, 3f), new Color(0.95f, 0.26f, 0.12f), 1.4f, 8f, LightmapBakeType.Realtime);
        MakePointLight("ControlRoomLight", lighting, new Vector3(3f, 5f, 50f), new Color(0.12f, 0.55f, 1f), 1.2f, 7f, LightmapBakeType.Realtime);

        SetupVolume();
        FirstPersonController playerController = SetupPlayer();
        SetupAmbientMusic();
        PlayerCheckpoint playerCheckpoint = Object.FindAnyObjectByType<PlayerCheckpoint>();
        checkpointTrigger.SetPlayerCheckpoint(playerCheckpoint);
        CanvasGroup completionOverlay = SetupPrompt(coinManager, playerCheckpoint, out TMP_Text securityStatusText);
        SetupIntroSequence(playerController, Object.FindAnyObjectByType<RunRecordManager>());
        SetupSecurityDrone(props, metal, trim, hazard, playerController, securityStatusText);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.1f, 0.14f);
        RenderSettings.ambientIntensity = 0.8f;

        // A small platform lies beyond the physical gate; crossing it completes the level.
        PbCube("ExitPlatform", architecture, new Vector3(0f, -0.25f, 66f), new Vector3(8f, 0.5f, 6f), floor);
        PbCube("FinishPillar_Left", architecture, new Vector3(-3.5f, 1.7f, 66.5f), new Vector3(0.28f, 3.4f, 0.28f), trim);
        PbCube("FinishPillar_Right", architecture, new Vector3(3.5f, 1.7f, 66.5f), new Vector3(0.28f, 3.4f, 0.28f), trim);
        PbCube("FinishBeam", architecture, new Vector3(0f, 3.4f, 66.5f), new Vector3(7.28f, 0.28f, 0.28f), amber);
        GameObject finish = new GameObject("LevelCompleteTrigger");
        finish.transform.position = new Vector3(0f, 1.5f, 66.5f);
        BoxCollider finishCollider = finish.AddComponent<BoxCollider>();
        finishCollider.isTrigger = true;
        finishCollider.size = new Vector3(7f, 3f, 1f);
        LevelCompleteTrigger finishTrigger = finish.AddComponent<LevelCompleteTrigger>();
        SerializedObject finishSerialized = new SerializedObject(finishTrigger);
        finishSerialized.FindProperty("completionOverlay").objectReferenceValue = completionOverlay;
        finishSerialized.FindProperty("coinManager").objectReferenceValue = coinManager;
        finishSerialized.FindProperty("runRecordManager").objectReferenceValue = Object.FindAnyObjectByType<RunRecordManager>();
        finishSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = core;
        if (!Lightmapping.isRunning && Lightmapping.BakeAsync())
            Debug.Log("Blue Box PA4 generated. Baking static lighting in the background.");
        else
            Debug.LogWarning("Blue Box PA4 generated. Use Blue Box > Bake Static Lighting when Unity finishes importing assets.");
    }

    [MenuItem("Blue Box/Prepare PA4 Scene")]
    public static void PrepareExistingSceneForPA4()
    {
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
        {
            Debug.LogError($"Scene not found: {ScenePath}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject environment = GameObject.Find("ENVIRONMENT");
        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        GameObject hud = GameObject.Find("InteractionHUD");
        if (environment == null || player == null || hud == null)
        {
            Debug.LogError("Could not find ENVIRONMENT, player, or InteractionHUD in the saved scene.");
            return;
        }

        Transform props = environment.transform.Find("Props");
        if (props == null)
        {
            Debug.LogError("Could not find ENVIRONMENT/Props in the saved scene.");
            return;
        }

        TMP_Text missionText = hud.transform.Find("MissionObjective")?.GetComponent<TMP_Text>();
        if (missionText == null)
        {
            missionText = CreateHudLabel(hud.transform, "MissionObjective",
                "OBJETIVO: RECOGE LAS 7 CELDAS, ACTIVA EL NÚCLEO Y ESCAPA\nWASD Mover   |   Shift Correr   |   Espacio Saltar   |   E Interactuar",
                24f, new Color(0.86f, 0.94f, 1f));
            RectTransform missionRect = missionText.rectTransform;
            missionRect.anchorMin = new Vector2(0.5f, 1f);
            missionRect.anchorMax = new Vector2(0.5f, 1f);
            missionRect.pivot = new Vector2(0.5f, 1f);
            missionRect.anchoredPosition = new Vector2(0f, -202f);
            missionRect.sizeDelta = new Vector2(900f, 72f);
            missionText.fontSize = 18f;
            missionText.alignment = TextAlignmentOptions.Center;
        }

        TMP_Text statusText = hud.transform.Find("SecurityStatus")?.GetComponent<TMP_Text>();
        if (statusText == null)
        {
            statusText = CreateHudLabel(hud.transform, "SecurityStatus", "SENTINELA: PATRULLANDO", 24f,
                new Color(0.35f, 0.92f, 0.8f));
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(1f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(1f, 1f);
            statusRect.anchoredPosition = new Vector2(-28f, -24f);
            statusRect.sizeDelta = new Vector2(340f, 42f);
            statusText.fontSize = 20f;
            statusText.alignment = TextAlignmentOptions.Right;
        }

        if (props.Find("SecuritySentinel") == null)
        {
            Material metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Metal.mat");
            Material trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Trim.mat");
            Material hazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Hazard.mat");
            SetupSecurityDrone(props, metal, trim, hazard, player, statusText);
        }

        foreach (Light pointLight in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (pointLight.type == LightType.Point && pointLight.lightmapBakeType == LightmapBakeType.Realtime)
                pointLight.shadows = LightShadows.None;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("PA4 HUD and patrol/chase sentinela installed. Static lighting data was preserved.");
    }

    [MenuItem("Blue Box/Bake Static Lighting")]
    public static void BakeLighting()
    {
        if (Lightmapping.isRunning)
        {
            Debug.Log("A lighting bake is already running.");
            return;
        }
        if (!Lightmapping.BakeAsync())
            Debug.LogWarning("Unity could not start the light bake. Open Window > Rendering > Lighting and check the bake settings.");
    }

    private static void EnsureFolders()
    {
        foreach (string path in new[] {
            "Assets/_Project", "Assets/_Project/Scenes", "Assets/_Project/Prefabs",
            "Assets/_Project/Materials", "Assets/_Project/Settings", "Assets/_Project/Settings/Volumes",
            "Assets/_Project/Scripts", "Assets/_Project/Timeline", "Assets/_Project/Shaders", "Assets/_Project/Audio"
        })
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string name = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, name);
            }
    }

    private static Transform Group(string name, Transform parent = null)
    {
        GameObject group = new GameObject(name);
        if (parent != null) group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject PbCube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        ProBuilderMesh mesh = ShapeFactory.Instantiate<Cube>(PivotLocation.Center);
        GameObject cube = mesh.gameObject;
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = size;
        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        if (cube.GetComponent<Collider>() == null) cube.AddComponent<BoxCollider>();
        cube.isStatic = true;
        return cube;
    }

    private static Material MakeMaterial(string name, Color color, float metallic, float smoothness, bool emission = false)
    {
        string path = "Assets/_Project/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.4f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void MakeConsole(Transform parent, Vector3 position, Material body, Material accent)
    {
        const string path = "Assets/_Project/Prefabs/LabConsole.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            GameObject root = new GameObject("LabConsole");
            PbCube("ConsoleBase", root.transform, new Vector3(0f, 0f, 0f), new Vector3(1.8f, 1.1f, 0.75f), body);
            PbCube("ConsoleScreen", root.transform, new Vector3(0f, 0.85f, 0.18f), new Vector3(1.25f, 0.55f, 0.08f), accent);
            PbCube("ConsoleFoot", root.transform, new Vector3(0f, -0.42f, 0f), new Vector3(1.35f, 0.18f, 0.65f), body);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "LabConsole";
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        foreach (Transform child in instance.transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = child.name == "ConsoleScreen" ? accent : body;
        }
    }

    private static void MakeCrate(Transform parent, Vector3 position, Material body, Material trim)
    {
        GameObject crate = PbCube("StorageCrate", parent, position, new Vector3(0.9f, 1.1f, 0.9f), body);
        PbCube("CrateBand", parent, position + new Vector3(0f, 0.2f, -0.46f), new Vector3(0.75f, 0.12f, 0.04f), trim);
    }

    private static void MakeHazardBlock(Transform parent, Vector3 position, Vector3 size, Material body, Material hazard, Material stripeMaterial)
    {
        const string path = "Assets/_Project/Prefabs/TouchHazard.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            GameObject root = new GameObject("TouchHazard");
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = size;
            root.AddComponent<TouchHazard>();
            PbCube("HazardBody", root.transform, Vector3.zero, size, body);
            GameObject stripe = PbCube("HazardStripe", root.transform, new Vector3(0f, size.y * 0.24f, -size.z * 0.51f),
                new Vector3(size.x * 0.88f, 0.16f, 0.06f), stripeMaterial);
            GameObject lamp = PbCube("HazardLamp", root.transform, new Vector3(0f, size.y * 0.42f, 0f),
                new Vector3(size.x * 0.55f, 0.12f, size.z * 0.55f), hazard);
            Collider bodyCollider = root.GetComponent<Collider>();
            foreach (Collider childCollider in root.GetComponentsInChildren<Collider>())
                if (childCollider != bodyCollider) childCollider.enabled = false;
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
    }

    private static Material MakeReactorPulseMaterial(Material fallback)
    {
        const string graphPath = "Assets/_Project/Shaders/SG_ReactorPulse.shadergraph";
        const string materialPath = "Assets/_Project/Materials/M_ReactorPulse.mat";
        if (!File.Exists(graphPath))
        {
            string samplePath = null;
            foreach (string candidate in Directory.GetFiles("Library/PackageCache", "SquareWave.shadergraph", SearchOption.AllDirectories))
            {
                if (candidate.Replace('\\', '/').Contains("/Samples~/NodeReference/Math/"))
                {
                    samplePath = candidate;
                    break;
                }
            }
            if (samplePath == null)
                return fallback;
            string graph = File.ReadAllText(samplePath).Replace("Square Wave", "Reactor Pulse");
            graph = graph.Replace("Square Wave Node", "Reactor Energy Pulse Node");
            graph = graph.Replace("The Basics", "Reactor signal: time and pulse");
            graph = graph.Replace("Here we can see the square wave.  It turns on and holds at 1, then turns off and holds at 0.",
                "La señal del núcleo alterna con el tiempo para representar energía intermitente.");
            string[] graphRecords = graph.Split(new[] { "\r\n\r\n", "\n\n" }, System.StringSplitOptions.None);
            for (int i = 0; i < graphRecords.Length; i++)
                if (graphRecords[i].Contains("\"m_ObjectId\": \"951ffd75ed874e60b07db11284b33865\""))
                    graphRecords[i] = graphRecords[i].Replace("\"m_Name\": \"Float\"", "\"m_Name\": \"Reactor pulse phase\"")
                        .Replace("\"m_Value\": 0.0", "\"m_Value\": 0.65");
            graph = string.Join("\n\n", graphRecords);
            File.WriteAllText(graphPath, graph);
        }

        AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceSynchronousImport);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(graphPath);
        if (shader == null)
            shader = AssetDatabase.LoadAllAssetsAtPath(graphPath).OfType<Shader>().FirstOrDefault();
        if (shader == null)
            return fallback;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_ReactorPulse" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else material.shader = shader;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void MakeLabTable(Transform parent, Vector3 position, Material metal, Material glass)
    {
        PbCube("LabTableTop", parent, position, new Vector3(2.1f, 0.15f, 1.1f), glass);
        foreach (float x in new[] { -0.85f, 0.85f })
            foreach (float z in new[] { -0.4f, 0.4f })
                PbCube("LabTableLeg", parent, position + new Vector3(x, -0.55f, z), new Vector3(0.12f, 1f, 0.12f), metal);
    }

    private static void MakePipe(Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipe.name = "ServicePipe";
        pipe.transform.SetParent(parent, false);
        pipe.transform.localPosition = position;
        pipe.transform.localScale = scale;
        pipe.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = pipe.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        pipe.isStatic = true;
    }

    private static void MakeEnergyCell(Transform parent, Vector3 position, Material material, CoinManager manager)
    {
        GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cell.name = "EnergyCell";
        cell.transform.SetParent(parent, false);
        cell.transform.localPosition = position;
        cell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // A wider face and spherical trigger make the jump collectable forgiving without changing its height.
        cell.transform.localScale = new Vector3(1f, 0.16f, 1f);
        cell.GetComponent<Renderer>().sharedMaterial = material;
        Collider oldCollider = cell.GetComponent<Collider>();
        if (oldCollider != null) Object.DestroyImmediate(oldCollider);
        SphereCollider trigger = cell.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.82f;
        EnergyCellPickup pickup = cell.AddComponent<EnergyCellPickup>();
        SerializedObject serialized = new SerializedObject(pickup);
        serialized.FindProperty("coinManager").objectReferenceValue = manager;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        cell.AddComponent<EnergyCellBob>();
    }

    private static void MakeCanister(Transform parent, Vector3 position, Material body, Material cap)
    {
        GameObject canister = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        canister.name = "ChemicalCanister";
        canister.transform.SetParent(parent, false);
        canister.transform.localPosition = position;
        canister.transform.localScale = new Vector3(0.42f, 0.62f, 0.42f);
        canister.GetComponent<Renderer>().sharedMaterial = body;
        Collider bodyCollider = canister.GetComponent<Collider>();
        if (bodyCollider != null) Object.DestroyImmediate(bodyCollider);
        GameObject capObject = PbCube("CanisterCap", parent, position + Vector3.up * 0.67f, new Vector3(0.3f, 0.08f, 0.3f), cap);
        capObject.GetComponent<Collider>().enabled = false;
        canister.isStatic = true;
    }

    private static Light MakePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range, LightmapBakeType bakeType)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.lightmapBakeType = bakeType;
        // The small realtime point lights are decorative. Disabling their shadows cuts per-frame GPU cost.
        light.shadows = LightShadows.None;
        return light;
    }

    private static GameObject MakeSteam(Transform parent, Vector3 position)
    {
        GameObject fx = new GameObject("SteamVent");
        fx.transform.SetParent(parent, false);
        fx.transform.localPosition = position;
        ParticleSystem particles = fx.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 1.2f;
        main.startSize = 0.18f;
        main.startColor = new Color(0.55f, 0.85f, 0.95f, 0.55f);
        var emission = particles.emission;
        emission.rateOverTime = 18f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 16f;
        shape.radius = 0.12f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        const string steamMaterialPath = "Assets/_Project/Materials/M_Steam.mat";
        Material steamMaterial = AssetDatabase.LoadAssetAtPath<Material>(steamMaterialPath);
        if (steamMaterial == null)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
                particleShader = Shader.Find("Particles/Standard Unlit");

            if (particleShader != null)
            {
                steamMaterial = new Material(particleShader);
                steamMaterial.name = "M_Steam";
                if (steamMaterial.HasProperty("_Surface"))
                    steamMaterial.SetFloat("_Surface", 1f);
                if (steamMaterial.HasProperty("_ZWrite"))
                    steamMaterial.SetFloat("_ZWrite", 0f);
                steamMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                steamMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                AssetDatabase.CreateAsset(steamMaterial, steamMaterialPath);
            }
        }

        renderer.sharedMaterial = steamMaterial;
        if (steamMaterial == null)
            renderer.enabled = false;
        return fx;
    }

    private static void SetupVolume()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        Bloom bloom = profile.components.Find(c => c is Bloom) as Bloom;
        if (bloom == null) bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.35f);
        ColorAdjustments color = profile.components.Find(c => c is ColorAdjustments) as ColorAdjustments;
        if (color == null) color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(-0.2f);
        color.contrast.Override(12f);
        color.saturation.Override(-8f);
        Vignette vignette = profile.components.Find(c => c is Vignette) as Vignette;
        if (vignette == null) vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.18f);

        GameObject volumeObject = new GameObject("MoodVolume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    private static void EnsureAmbientMusicAsset()
    {
        if (File.Exists(AmbientMusicPath))
            return;

        // A quiet, original pad synthesized as a seamless 40-second chord loop; no external audio asset required.
        const int sampleRate = 22050;
        const int seconds = 40;
        const short channels = 2;
        const short bitsPerSample = 16;
        int sampleCount = sampleRate * seconds;
        int dataBytes = sampleCount * channels * (bitsPerSample / 8);
        using (BinaryWriter writer = new BinaryWriter(File.Create(AmbientMusicPath)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataBytes);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * (bitsPerSample / 8));
            writer.Write((short)(channels * (bitsPerSample / 8)));
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataBytes);

            for (int i = 0; i < sampleCount; i++)
            {
                double time = (double)i / sampleRate;
                double breathe = 0.88 + 0.12 * System.Math.Sin(2.0 * System.Math.PI * 0.05 * time);
                double pad = breathe * (0.055 * System.Math.Sin(2.0 * System.Math.PI * 55.0 * time)
                    + 0.045 * System.Math.Sin(2.0 * System.Math.PI * 110.0 * time)
                    + 0.035 * System.Math.Sin(2.0 * System.Math.PI * 165.0 * time)
                    + 0.026 * System.Math.Sin(2.0 * System.Math.PI * 220.0 * time)
                    + 0.018 * System.Math.Sin(2.0 * System.Math.PI * 247.5 * time));
                short sample = (short)System.Math.Round(Mathf.Clamp((float)pad, -1f, 1f) * short.MaxValue);
                writer.Write(sample);
                writer.Write(sample);
            }
        }
        AssetDatabase.ImportAsset(AmbientMusicPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void SetupAmbientMusic()
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AmbientMusicPath);
        if (clip == null)
        {
            Debug.LogWarning("Ambient music could not be imported from " + AmbientMusicPath);
            return;
        }

        GameObject musicObject = new GameObject("AmbientMusic");
        AudioSource source = musicObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.volume = 0.35f;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
    }

    private static FirstPersonController SetupPlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0.1f, 1f);
        CharacterController body = player.AddComponent<CharacterController>();
        body.height = 1.8f;
        body.radius = 0.32f;
        body.center = new Vector3(0f, 0.9f, 0f);
        body.stepOffset = 0.42f;
        FirstPersonController movement = player.AddComponent<FirstPersonController>();
        player.AddComponent<PlayerCheckpoint>();

        GameObject aimObject = new GameObject("CameraAimTarget");
        aimObject.transform.SetParent(player.transform, false);
        aimObject.transform.localPosition = new Vector3(0f, 1.58f, 0f);

        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<CinemachineBrain>();
        SerializedObject movementSerialized = new SerializedObject(movement);
        movementSerialized.FindProperty("cameraPivot").objectReferenceValue = aimObject.transform;
        movementSerialized.ApplyModifiedPropertiesWithoutUndo();

        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        SerializedObject interactorSerialized = new SerializedObject(interactor);
        interactorSerialized.FindProperty("viewCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
        interactorSerialized.ApplyModifiedPropertiesWithoutUndo();
        return movement;
    }

    private static void SetupIntroSequence(FirstPersonController playerController, RunRecordManager runRecordManager)
    {
        Camera physicalCamera = Camera.main;
        if (physicalCamera == null || playerController == null)
            return;
        CinemachineBrain brain = physicalCamera.GetComponent<CinemachineBrain>();
        if (brain == null) brain = physicalCamera.gameObject.AddComponent<CinemachineBrain>();

        Transform aimTarget = playerController.transform.Find("CameraAimTarget");
        GameObject gameplayCameraObject = new GameObject("GameplayCinemachineCamera");
        CinemachineCamera gameplayCamera = gameplayCameraObject.AddComponent<CinemachineCamera>();
        gameplayCamera.Follow = aimTarget;
        gameplayCamera.Priority = 10;
        gameplayCameraObject.AddComponent<CinemachineHardLockToTarget>();
        gameplayCameraObject.AddComponent<CinemachineRotateWithFollowTarget>();

        GameObject introCameraObject = new GameObject("IntroCinemachineCamera");
        introCameraObject.transform.position = new Vector3(0f, 4.5f, -5f);
        CinemachineCamera introCamera = introCameraObject.AddComponent<CinemachineCamera>();
        introCamera.Priority = 20;
        Animator animator = introCameraObject.AddComponent<Animator>();

        const string timelinePath = "Assets/_Project/Timeline/IntroCinematic.playable";
        const string animationPath = "Assets/_Project/Timeline/IntroCameraMove.anim";
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
        if (timeline == null)
        {
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, timelinePath);
        }
        else
        {
            foreach (TrackAsset oldTrack in timeline.GetOutputTracks().ToArray())
                timeline.DeleteTrack(oldTrack);
        }

        CinemachineTrack cameraTrack = timeline.CreateTrack<CinemachineTrack>(null, "Opening flythrough");
        TimelineClip shotClip = cameraTrack.CreateDefaultClip();
        shotClip.displayName = "Recorrido del laboratorio";
        shotClip.duration = 8.0;
        CinemachineShot shot = shotClip.asset as CinemachineShot;
        if (shot != null)
            shot.VirtualCamera.defaultValue = introCamera;

        AnimationClip animation = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);
        if (animation == null)
        {
            animation = new AnimationClip { name = "IntroCameraMove", frameRate = 30f };
            SetTransformCurve(animation, "m_LocalPosition.x", new[] { 0f, 2.5f, 5.5f, 8f }, new[] { 0f, -4f, 3f, 0f });
            SetTransformCurve(animation, "m_LocalPosition.y", new[] { 0f, 2.5f, 5.5f, 8f }, new[] { 4.5f, 5.5f, 4.2f, 3.2f });
            SetTransformCurve(animation, "m_LocalPosition.z", new[] { 0f, 2.5f, 5.5f, 8f }, new[] { -5f, 17f, 39f, 56f });
            SetTransformCurve(animation, "localEulerAnglesRaw.x", new[] { 0f, 2.5f, 5.5f, 8f }, new[] { 15f, 12f, 10f, 4f });
            SetTransformCurve(animation, "localEulerAnglesRaw.y", new[] { 0f, 2.5f, 5.5f, 8f }, new[] { 0f, -8f, 5f, 0f });
            SetTransformCurve(animation, "localEulerAnglesRaw.z", new[] { 0f, 2.5f, 5.5f, 8f }, new[] { 0f, 0f, 0f, 0f });
            AssetDatabase.CreateAsset(animation, animationPath);
        }
        AnimationTrack animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Camera movement");
        TimelineClip movementClip = animationTrack.CreateClip(animation);
        movementClip.displayName = "Vuelo de entrada";
        movementClip.duration = 8.0;
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = 8.0;
        EditorUtility.SetDirty(timeline);

        GameObject directorObject = new GameObject("IntroSequenceDirector");
        PlayableDirector director = directorObject.AddComponent<PlayableDirector>();
        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.SetGenericBinding(cameraTrack, brain);
        director.SetGenericBinding(animationTrack, animator);
        IntroSequenceController sequence = directorObject.AddComponent<IntroSequenceController>();
        SerializedObject sequenceSerialized = new SerializedObject(sequence);
        sequenceSerialized.FindProperty("director").objectReferenceValue = director;
        sequenceSerialized.FindProperty("playerController").objectReferenceValue = playerController;
        sequenceSerialized.FindProperty("introCamera").objectReferenceValue = introCamera;
        sequenceSerialized.FindProperty("runRecordManager").objectReferenceValue = runRecordManager;
        sequenceSerialized.ApplyModifiedProperties();
    }

    private static void SetTransformCurve(AnimationClip clip, string property, float[] times, float[] values)
    {
        Keyframe[] keys = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
            keys[i] = new Keyframe(times[i], values[i]);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), property),
            new AnimationCurve(keys));
    }

    private static CanvasGroup SetupPrompt(CoinManager coinManager, PlayerCheckpoint playerCheckpoint, out TMP_Text securityStatusText)
    {
        GameObject canvasObject = new GameObject("InteractionHUD", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        GameObject label = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.15f);
        rect.anchorMax = new Vector2(0.5f, 0.15f);
        rect.sizeDelta = new Vector2(420f, 48f);
        rect.anchoredPosition = Vector2.zero;
        TMP_Text text = label.GetComponent<TMP_Text>();
        text.text = string.Empty;
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.Center;

        TMP_Text missionText = CreateHudLabel(canvasObject.transform, "MissionObjective",
            "OBJETIVO: RECOGE LAS 7 CELDAS, ACTIVA EL NÚCLEO Y ESCAPA\nWASD Mover   |   Shift Correr   |   Espacio Saltar   |   E Interactuar",
            24f, new Color(0.86f, 0.94f, 1f));
        RectTransform missionRect = missionText.rectTransform;
        missionRect.anchorMin = new Vector2(0.5f, 1f);
        missionRect.anchorMax = new Vector2(0.5f, 1f);
        missionRect.pivot = new Vector2(0.5f, 1f);
        missionRect.anchoredPosition = new Vector2(0f, -202f);
        missionRect.sizeDelta = new Vector2(900f, 72f);
        missionText.fontSize = 18f;
        missionText.alignment = TextAlignmentOptions.Center;

        securityStatusText = CreateHudLabel(canvasObject.transform, "SecurityStatus", "SENTINELA: PATRULLANDO", 24f,
            new Color(0.35f, 0.92f, 0.8f));
        RectTransform securityRect = securityStatusText.rectTransform;
        securityRect.anchorMin = new Vector2(1f, 1f);
        securityRect.anchorMax = new Vector2(1f, 1f);
        securityRect.pivot = new Vector2(1f, 1f);
        securityRect.anchoredPosition = new Vector2(-28f, -24f);
        securityRect.sizeDelta = new Vector2(340f, 42f);
        securityStatusText.fontSize = 20f;
        securityStatusText.alignment = TextAlignmentOptions.Right;

        GameObject counter = new GameObject("EnergyCellCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
        counter.transform.SetParent(canvasObject.transform, false);
        RectTransform counterRect = counter.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0f, 1f);
        counterRect.anchorMax = new Vector2(0f, 1f);
        counterRect.pivot = new Vector2(0f, 1f);
        counterRect.anchoredPosition = new Vector2(28f, -24f);
        counterRect.sizeDelta = new Vector2(300f, 48f);
        TMP_Text counterText = counter.GetComponent<TMP_Text>();
        counterText.fontSize = 26f;
        counterText.color = new Color(1f, 0.78f, 0.18f);
        counterText.alignment = TextAlignmentOptions.Left;
        SerializedObject managerSerialized = new SerializedObject(coinManager);
        managerSerialized.FindProperty("counterText").objectReferenceValue = counterText;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();
        coinManager.RefreshCounter();

        TMP_Text timerText = CreateHudLabel(canvasObject.transform, "RunTimer", "Tiempo: 00:00.00", 110f,
            new Color(0.82f, 0.92f, 1f));
        TMP_Text recordText = CreateHudLabel(canvasObject.transform, "BestRecord", "Récord: --:--.--", 152f,
            new Color(0.45f, 0.85f, 1f));

        GameObject lives = new GameObject("LivesCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
        lives.transform.SetParent(canvasObject.transform, false);
        RectTransform livesRect = lives.GetComponent<RectTransform>();
        livesRect.anchorMin = new Vector2(0f, 1f);
        livesRect.anchorMax = new Vector2(0f, 1f);
        livesRect.pivot = new Vector2(0f, 1f);
        livesRect.anchoredPosition = new Vector2(28f, -66f);
        livesRect.sizeDelta = new Vector2(300f, 42f);
        TMP_Text livesText = lives.GetComponent<TMP_Text>();
        livesText.fontSize = 24f;
        livesText.color = new Color(1f, 0.43f, 0.32f);
        livesText.alignment = TextAlignmentOptions.Left;
        CanvasGroup gameOverOverlay = CreateGameOverOverlay(canvasObject.transform);
        SerializedObject checkpointSerialized = new SerializedObject(playerCheckpoint);
        checkpointSerialized.FindProperty("livesCounter").objectReferenceValue = livesText;
        checkpointSerialized.FindProperty("gameOverOverlay").objectReferenceValue = gameOverOverlay;
        checkpointSerialized.ApplyModifiedPropertiesWithoutUndo();

        PlayerInteractor interactor = Object.FindAnyObjectByType<PlayerInteractor>();
        SerializedObject serialized = new SerializedObject(interactor);
        serialized.FindProperty("promptText").objectReferenceValue = text;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        CanvasGroup completionOverlay = CreateCompletionOverlay(canvasObject.transform, out TMP_Text completionResultText);
        RunRecordManager recordManager = canvasObject.AddComponent<RunRecordManager>();
        SerializedObject recordSerialized = new SerializedObject(recordManager);
        recordSerialized.FindProperty("timerText").objectReferenceValue = timerText;
        recordSerialized.FindProperty("bestRecordText").objectReferenceValue = recordText;
        recordSerialized.FindProperty("completionResultText").objectReferenceValue = completionResultText;
        recordSerialized.ApplyModifiedPropertiesWithoutUndo();
        return completionOverlay;
    }

    private static void SetupSecurityDrone(Transform props, Material metal, Material trim, Material hazard,
        FirstPersonController player, TMP_Text statusText)
    {
        GameObject route = new GameObject("SentinelPatrolRoute");
        route.transform.SetParent(props, false);
        Transform waypointA = new GameObject("PatrolWaypointA").transform;
        waypointA.SetParent(route.transform, false);
        waypointA.localPosition = new Vector3(0f, 0.04f, 37.5f);
        Transform waypointB = new GameObject("PatrolWaypointB").transform;
        waypointB.SetParent(route.transform, false);
        waypointB.localPosition = new Vector3(0f, 0.04f, 44.5f);

        GameObject drone = new GameObject("SecuritySentinel");
        drone.transform.SetParent(props, false);
        drone.transform.localPosition = waypointA.localPosition;
        drone.transform.localRotation = Quaternion.identity;
        MakeDronePart(drone.transform, PrimitiveType.Cube, "ArmorCore", new Vector3(0f, 1.05f, 0f),
            new Vector3(0.9f, 0.8f, 0.65f), metal);
        MakeDronePart(drone.transform, PrimitiveType.Sphere, "SensorHead", new Vector3(0f, 1.68f, 0.06f),
            new Vector3(0.62f, 0.43f, 0.56f), metal);
        MakeDronePart(drone.transform, PrimitiveType.Sphere, "Optic", new Vector3(0f, 1.67f, 0.34f),
            new Vector3(0.34f, 0.12f, 0.1f), hazard);
        MakeDronePart(drone.transform, PrimitiveType.Cube, "LeftArm", new Vector3(-0.6f, 1.05f, 0f),
            new Vector3(0.22f, 0.68f, 0.26f), trim);
        MakeDronePart(drone.transform, PrimitiveType.Cube, "RightArm", new Vector3(0.6f, 1.05f, 0f),
            new Vector3(0.22f, 0.68f, 0.26f), trim);
        MakeDronePart(drone.transform, PrimitiveType.Capsule, "LeftLeg", new Vector3(-0.24f, 0.58f, 0f),
            new Vector3(0.22f, 0.58f, 0.22f), metal);
        MakeDronePart(drone.transform, PrimitiveType.Capsule, "RightLeg", new Vector3(0.24f, 0.58f, 0f),
            new Vector3(0.22f, 0.58f, 0.22f), metal);

        GameObject lightObject = new GameObject("AlertLight");
        lightObject.transform.SetParent(drone.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 2.02f, 0f);
        Light alertLight = lightObject.AddComponent<Light>();
        alertLight.type = LightType.Point;
        alertLight.color = new Color(1f, 0.22f, 0.08f);
        alertLight.intensity = 0.45f;
        alertLight.range = 5f;
        alertLight.shadows = LightShadows.None;

        AudioClip alertClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Sentinel_Alert.wav");
        AudioSource audioSource = drone.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.25f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 18f;

        SecurityDroneAI ai = drone.AddComponent<SecurityDroneAI>();
        SerializedObject serialized = new SerializedObject(ai);
        serialized.FindProperty("player").objectReferenceValue = player.transform;
        serialized.FindProperty("playerCheckpoint").objectReferenceValue = player.GetComponent<PlayerCheckpoint>();
        serialized.FindProperty("waypointA").objectReferenceValue = waypointA;
        serialized.FindProperty("waypointB").objectReferenceValue = waypointB;
        serialized.FindProperty("statusText").objectReferenceValue = statusText;
        serialized.FindProperty("alertLight").objectReferenceValue = alertLight;
        serialized.FindProperty("alertAudio").objectReferenceValue = audioSource;
        serialized.FindProperty("alertClip").objectReferenceValue = alertClip;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject MakeDronePart(Transform parent, PrimitiveType primitive, string name, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            Object.DestroyImmediate(partCollider);
        return part;
    }

    private static TMP_Text CreateHudLabel(Transform parent, string name, string value, float topOffset, Color color)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(28f, -topOffset);
        rect.sizeDelta = new Vector2(340f, 42f);
        TMP_Text text = label.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = 24f;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    private static CanvasGroup CreateGameOverOverlay(Transform parent)
    {
        GameObject overlay = new GameObject("GameOverOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(parent, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.08f, 0.015f, 0.025f, 0.94f);
        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject card = new GameObject("GameOverCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(overlay.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(700f, 350f);
        card.GetComponent<Image>().color = new Color(0.12f, 0.035f, 0.055f, 1f);
        AddCompletionText(card.transform, "SIN VIDAS", 54f, new Color(1f, 0.35f, 0.28f),
            TextAlignmentOptions.Center, new Vector2(0f, 55f), new Vector2(620f, 80f));
        AddCompletionText(card.transform, "Tocaste demasiados obstáculos.\nPulsa R para reiniciar el nivel.", 25f,
            new Color(0.96f, 0.88f, 0.88f), TextAlignmentOptions.Center, new Vector2(0f, -55f), new Vector2(620f, 110f));
        return group;
    }

    private static CanvasGroup CreateCompletionOverlay(Transform parent, out TMP_Text completionResultText)
    {
        GameObject overlay = new GameObject("LevelCompleteOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(parent, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.075f, 0.94f);
        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject card = new GameObject("CompletionCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(overlay.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 430f);
        card.GetComponent<Image>().color = new Color(0.035f, 0.09f, 0.16f, 1f);

        GameObject accent = new GameObject("CompletionAccent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(card.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 10f);
        accent.GetComponent<Image>().color = new Color(0.04f, 0.78f, 0.95f, 1f);

        GameObject emblem = new GameObject("CompletionEmblem", typeof(RectTransform), typeof(Image));
        emblem.transform.SetParent(card.transform, false);
        RectTransform emblemRect = emblem.GetComponent<RectTransform>();
        emblemRect.anchorMin = new Vector2(0.5f, 0.72f);
        emblemRect.anchorMax = new Vector2(0.5f, 0.72f);
        emblemRect.sizeDelta = new Vector2(94f, 94f);
        emblem.GetComponent<Image>().color = new Color(0.04f, 0.78f, 0.95f, 1f);
        AddCompletionText(emblem.transform, "7", 64f, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(94f, 94f));
        AddCompletionText(card.transform, "NIVEL COMPLETADO", 46f, new Color(1f, 0.78f, 0.18f), TextAlignmentOptions.Center,
            new Vector2(0f, -35f), new Vector2(680f, 72f));
        AddCompletionText(card.transform, "Has atravesado la salida y recuperado la energía del laboratorio.", 22f,
            new Color(0.82f, 0.9f, 0.96f), TextAlignmentOptions.Center, new Vector2(0f, -105f), new Vector2(650f, 70f));
        completionResultText = AddCompletionText(card.transform, "Tiempo: 00:00.00", 21f, new Color(0.3f, 0.9f, 1f),
            TextAlignmentOptions.Center, new Vector2(0f, -170f), new Vector2(620f, 64f));
        return group;
    }

    private static TMP_Text AddCompletionText(Transform parent, string value, float fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        TMP_Text text = label.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
}
#endif
