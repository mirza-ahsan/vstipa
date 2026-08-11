using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

public static class BuildScript
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string AvatarPath = "Assets/Avatars/male_avatar.glb";
    private const string MaterialDirectory = "Assets/Generated/Materials";
    private const string XrSettingsDirectory = "Assets/XR/Settings";
    private const string XrGeneralSettingsPath = XrSettingsDirectory + "/XRGeneralSettingsPerBuildTarget.asset";
    private const string OpenXrLoaderPath = XrSettingsDirectory + "/OpenXRLoader.asset";

    private static readonly Color Navy = Html("#091421");
    private static readonly Color Panel = Html("#122338");
    private static readonly Color Cream = Html("#F6F0E4");
    private static readonly Color Muted = Html("#A9B7C6");
    private static readonly Color Warm = Html("#F2A65A");
    private static readonly Color Stern = Html("#E56B6F");
    private static readonly Color Neutral = Html("#58C7C1");

    [MenuItem("V-STIPA/Setup Main Scene")]
    public static void SetupMainScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MaterialDirectory);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateRoom();
        bool questTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
        Camera camera = CreateCamera(questTarget);
        CreateLighting();

        GameObject systems = new GameObject("ExperienceSystems");
        AudioSource audioSource = systems.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        QuestionPlaybackController controller = systems.AddComponent<QuestionPlaybackController>();
        controller.audioSource = audioSource;
        controller.loadOnStart = false;
        controller.autoStartOnManifestLoad = true;

        uLipSync.uLipSync analyzer = systems.AddComponent<uLipSync.uLipSync>();
        analyzer.profile = AssetDatabase.LoadAssetAtPath<uLipSync.Profile>(
            "Packages/com.hecomi.ulipsync/Assets/Profiles/uLipSync-Profile-Sample-Male.asset");
        analyzer.outputSoundGain = 1f;

        GameObject avatarStage = new GameObject("AvatarStage");
        PersonaManager.PersonaSlot warm = CreatePersona(avatarStage.transform, "warm", "Warm & Encouraging", Warm, 25f, audioSource);
        PersonaManager.PersonaSlot stern = CreatePersona(avatarStage.transform, "stern", "Stern & Challenging", Stern, 0f, audioSource);
        PersonaManager.PersonaSlot neutral = CreatePersona(avatarStage.transform, "neutral", "Neutral & Professional", Neutral, 8f, audioSource);

        PersonaManager personaManager = systems.AddComponent<PersonaManager>();
        personaManager.playbackController = controller;
        personaManager.personas = new[] { warm, stern, neutral };

        ULipSyncRouter router = systems.AddComponent<ULipSyncRouter>();
        router.analyzer = analyzer;
        router.targets = new[] { warm.lipSync, stern.lipSync, neutral.lipSync };

        CreateEventSystem();
        CreateInterface(font, controller, personaManager, camera);

        QuestRuntimeAdapter questRuntime = systems.AddComponent<QuestRuntimeAdapter>();
        questRuntime.headsetCamera = camera;
        questRuntime.interfaceCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        questRuntime.playbackUI = UnityEngine.Object.FindAnyObjectByType<PlaybackUI>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildScript] MainScene created: interview room, three selectable persona presets, uLipSync routing, and completion flow.");
    }

    private static Camera CreateCamera(bool questTarget)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 1.46f, -2.65f);
        camera.transform.LookAt(new Vector3(-0.62f, 1.34f, 1.12f));
        camera.fieldOfView = 46f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Html("#E8E0D3");
        camera.nearClipPlane = 0.05f;
        go.AddComponent<AudioListener>();

        if (questTarget)
        {
            GameObject origin = new GameObject("XR Origin (Seated)");
            origin.transform.position = camera.transform.position;
            origin.transform.rotation = camera.transform.rotation;
            camera.transform.SetParent(origin.transform, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            TrackedPoseDriver poseDriver = go.AddComponent<TrackedPoseDriver>();
            poseDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
        }
        return camera;
    }

    private static void CreateLighting()
    {
        GameObject key = new GameObject("Key Light");
        Light keyLight = key.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.94f, 0.84f);
        keyLight.intensity = 0.92f;
        key.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

        GameObject fill = new GameObject("Window Fill");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(0.62f, 0.78f, 1f);
        fillLight.intensity = 1.15f;
        fillLight.range = 6f;
        fill.transform.position = new Vector3(-2.4f, 2.5f, 0.2f);

        GameObject practical = new GameObject("Warm Practical Light");
        Light practicalLight = practical.AddComponent<Light>();
        practicalLight.type = LightType.Point;
        practicalLight.color = new Color(1f, 0.67f, 0.38f);
        practicalLight.intensity = 0.75f;
        practicalLight.range = 4.5f;
        practical.transform.position = new Vector3(2.25f, 2.35f, 2.7f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.36f, 0.33f);
    }

    private static void CreateRoom()
    {
        Material floor = Material("Warm Oak Floor", Html("#80634B"));
        Material wall = Material("Warm Ivory Wall", Html("#E7DFD2"));
        Material navy = Material("Executive Navy", Html("#17324A"));
        Material navySoft = Material("Executive Navy Soft", Html("#294A62"));
        Material oak = Material("Light Oak", Html("#B78355"));
        Material oakDark = Material("Walnut Trim", Html("#5A3B2B"));
        Material leather = Material("Chair Leather", Html("#24303B"));
        Material metal = Material("Brushed Metal", Html("#B8BEC2"));
        Material green = Material("Plant Green", Html("#3F7658"));
        Material greenLight = Material("Plant Highlight", Html("#6B9A73"));
        Material rug = Material("Office Rug", Html("#C9BBA7"));
        Material window = Material("Window Glass", Html("#789BB3"));
        Material paper = Material("Certificate Paper", Html("#F7F0E3"));

        Primitive("Warm Oak Floor", PrimitiveType.Cube, new Vector3(0f, -0.08f, 1f), new Vector3(8f, 0.16f, 8f), floor);
        Primitive("Warm Ivory Back Wall", PrimitiveType.Cube, new Vector3(0f, 2.25f, 4f), new Vector3(8f, 4.5f, 0.15f), wall);
        Primitive("Warm Ivory Left Wall", PrimitiveType.Cube, new Vector3(-4f, 2.25f, 1f), new Vector3(0.15f, 4.5f, 6f), wall);
        Primitive("Walnut Baseboard", PrimitiveType.Cube, new Vector3(0f, 0.12f, 3.86f), new Vector3(8f, 0.18f, 0.08f), oakDark);
        Primitive("Office Rug", PrimitiveType.Cube, new Vector3(0f, 0.015f, 0.65f), new Vector3(4.7f, 0.03f, 3.6f), rug);

        // A dark architectural feature wall and oak slats give the interviewer a
        // deliberate visual frame instead of the old flat grey backdrop.
        Primitive("Executive Feature Wall", PrimitiveType.Cube, new Vector3(-0.65f, 1.82f, 3.82f),
            new Vector3(3.5f, 3.25f, 0.07f), navy);
        for (int i = 0; i < 10; i++)
        {
            float x = -2.28f + i * 0.18f;
            Primitive($"Oak Slat {i + 1:D2}", PrimitiveType.Cube, new Vector3(x, 1.82f, 3.72f),
                new Vector3(0.055f, 3.05f, 0.055f), oak);
        }

        // Window and skyline blocks provide depth and a professional office cue.
        Primitive("Office Window", PrimitiveType.Cube, new Vector3(2.35f, 2.35f, 3.82f), new Vector3(2.35f, 1.65f, 0.06f), window);
        Primitive("Window Header", PrimitiveType.Cube, new Vector3(2.35f, 3.22f, 3.72f), new Vector3(2.5f, 0.10f, 0.08f), metal);
        Primitive("Window Sill", PrimitiveType.Cube, new Vector3(2.35f, 1.48f, 3.72f), new Vector3(2.5f, 0.10f, 0.08f), metal);
        Primitive("Window Mullion", PrimitiveType.Cube, new Vector3(2.35f, 2.35f, 3.71f), new Vector3(0.07f, 1.65f, 0.08f), metal);
        Primitive("Skyline 1", PrimitiveType.Cube, new Vector3(1.65f, 1.84f, 3.68f), new Vector3(0.38f, 0.65f, 0.07f), navySoft);
        Primitive("Skyline 2", PrimitiveType.Cube, new Vector3(2.15f, 1.96f, 3.68f), new Vector3(0.46f, 0.9f, 0.07f), navySoft);
        Primitive("Skyline 3", PrimitiveType.Cube, new Vector3(2.75f, 1.76f, 3.68f), new Vector3(0.52f, 0.5f, 0.07f), navySoft);

        // Slim executive desk: no solid front panel, so it no longer hides the avatar.
        GameObject desk = new GameObject("Slim Executive Interview Desk");
        Primitive("Oak Desktop", PrimitiveType.Cube, new Vector3(-0.12f, 0.61f, 0.42f), new Vector3(2.65f, 0.09f, 0.66f), oak, desk.transform);
        Primitive("Left Metal Leg", PrimitiveType.Cube, new Vector3(-1.23f, 0.3f, 0.48f), new Vector3(0.08f, 0.58f, 0.48f), metal, desk.transform);
        Primitive("Right Metal Leg", PrimitiveType.Cube, new Vector3(0.99f, 0.3f, 0.48f), new Vector3(0.08f, 0.58f, 0.48f), metal, desk.transform);
        Primitive("Desk Modesty Rail", PrimitiveType.Cube, new Vector3(-0.12f, 0.37f, 0.70f), new Vector3(2.0f, 0.08f, 0.06f), oakDark, desk.transform);
        Primitive("Desk Pad", PrimitiveType.Cube, new Vector3(-0.55f, 0.665f, 0.36f), new Vector3(0.76f, 0.018f, 0.36f), leather, desk.transform);

        // Proper seated interviewer chair, visible around the avatar's shoulders.
        GameObject interviewerChair = new GameObject("Interviewer Executive Chair");
        Primitive("Leather Seat", PrimitiveType.Cube, new Vector3(-0.63f, 0.43f, 1.18f), new Vector3(0.86f, 0.13f, 0.72f), leather, interviewerChair.transform);
        Primitive("Leather Back", PrimitiveType.Capsule, new Vector3(-0.63f, 1.08f, 1.47f), new Vector3(0.78f, 0.68f, 0.16f), leather, interviewerChair.transform);
        Primitive("Left Armrest", PrimitiveType.Cube, new Vector3(-1.10f, 0.71f, 1.11f), new Vector3(0.09f, 0.09f, 0.56f), leather, interviewerChair.transform);
        Primitive("Right Armrest", PrimitiveType.Cube, new Vector3(-0.16f, 0.71f, 1.11f), new Vector3(0.09f, 0.09f, 0.56f), leather, interviewerChair.transform);
        Primitive("Chair Column", PrimitiveType.Cylinder, new Vector3(-0.63f, 0.22f, 1.18f), new Vector3(0.10f, 0.22f, 0.10f), metal, interviewerChair.transform);
        Primitive("Chair Base", PrimitiveType.Cylinder, new Vector3(-0.63f, 0.08f, 1.18f), new Vector3(0.42f, 0.035f, 0.42f), metal, interviewerChair.transform);

        // Framed credentials and restrained accessories finish the office set.
        Primitive("Credential Frame", PrimitiveType.Cube, new Vector3(-3.12f, 2.35f, 3.73f), new Vector3(0.72f, 0.92f, 0.06f), oakDark);
        Primitive("Credential", PrimitiveType.Cube, new Vector3(-3.12f, 2.35f, 3.65f), new Vector3(0.59f, 0.78f, 0.035f), paper);
        Primitive("Credential Seal", PrimitiveType.Cylinder, new Vector3(-3.12f, 2.18f, 3.60f), new Vector3(0.09f, 0.018f, 0.09f), Warm);
        Primitive("Plant Pot", PrimitiveType.Cylinder, new Vector3(2.85f, 0.34f, 2.85f), new Vector3(0.38f, 0.34f, 0.38f), oakDark);
        Primitive("Plant Crown", PrimitiveType.Sphere, new Vector3(2.85f, 1.05f, 2.85f), new Vector3(0.70f, 1.08f, 0.70f), green);
        Primitive("Plant Highlight", PrimitiveType.Sphere, new Vector3(2.58f, 1.13f, 2.73f), new Vector3(0.38f, 0.72f, 0.38f), greenLight);

        // The candidate's chair is intentionally outside this camera composition.
        // A foreground chair back obscures the desk and reads as a large dark blob
        // in the flat-screen recording used for the submission.
    }

    private static PersonaManager.PersonaSlot CreatePersona(
        Transform parent, string id, string displayName, Color accent, float smile, AudioSource audioSource)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);
        GameObject avatar;
        if (prefab != null)
        {
            avatar = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
        else
        {
            avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.transform.SetParent(parent, false);
            Debug.LogError($"[BuildScript] Missing avatar at {AvatarPath}; scene contains a diagnostic capsule.");
        }

        avatar.name = $"{displayName} Avatar (T1 Fallback)";
        // Lower the standing fallback into the executive chair so it reads as a
        // seated interviewer while preserving the native rig and idle animation.
        avatar.transform.position = new Vector3(-0.63f, -0.48f, 1.10f);
        avatar.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        avatar.transform.localScale = Vector3.one * 1.35f;

        HumanoidAvatarConfigurator humanoid = avatar.AddComponent<HumanoidAvatarConfigurator>();
        humanoid.animator = avatar.GetComponentInChildren<Animator>(true);
        // This T1 GLB contains a native generic idle clip. Assigning a runtime
        // Humanoid Avatar before playing that clip corrupts its skin pose, so keep
        // the mapper available for the future retargetable exports but do not force
        // conversion on this fallback model.
        humanoid.configureOnAwake = false;
        if (humanoid.animator != null) humanoid.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (SkinnedMeshRenderer renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            renderer.enabled = true;
            renderer.updateWhenOffscreen = true;
        }

        AvatarGestureController gesture = avatar.AddComponent<AvatarGestureController>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(AvatarPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            {
                gesture.idleClip = clip;
                break;
            }
        }

        AvatarLipSync lipSync = avatar.AddComponent<AvatarLipSync>();
        lipSync.audioSource = audioSource;
        lipSync.mouthProxy = CreateMouthProxy(avatar.transform, accent);

        GameObject badge = Primitive($"{id} persona badge", PrimitiveType.Cube,
            new Vector3(0.17f, 1.45f, -0.19f), new Vector3(0.06f, 0.11f, 0.015f), accent, avatar.transform, true);
        badge.transform.localRotation = Quaternion.identity;

        avatar.SetActive(false);
        return new PersonaManager.PersonaSlot
        {
            persona = id,
            displayName = displayName,
            avatarRoot = avatar,
            gestureController = gesture,
            lipSync = lipSync,
            accentColor = accent,
            baselineSmile = smile
        };
    }

    private static Transform CreateMouthProxy(Transform avatarRoot, Color accent)
    {
        Transform head = FindDescendant(avatarRoot, "Head");
        Transform parent = head != null ? head : avatarRoot;
        GameObject proxy = Primitive("Static Face Mouth Fallback", PrimitiveType.Sphere,
            new Vector3(0f, 0.035f, 0.105f), new Vector3(0.055f, 0.008f, 0.018f),
            Material("Mouth", Color.Lerp(Html("#321A20"), accent, 0.12f)), parent, true);
        return proxy.transform;
    }

    private static void CreateInterface(Font font, QuestionPlaybackController controller, PersonaManager personas, Camera camera)
    {
        GameObject canvasGo = new GameObject("Experience UI");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        Text brand = UIText("Brand", canvasGo.transform, font, "V-STIPA  •  INTERVIEW SIMULATOR", 20, Cream, TextAnchor.MiddleLeft,
            new Vector2(0.035f, 0.925f), new Vector2(0.52f, 0.985f));
        brand.fontStyle = FontStyle.Bold;

        GameObject start = UIPanel("Persona Selection", canvasGo.transform, new Color(Navy.r, Navy.g, Navy.b, 0.92f), Vector2.zero, Vector2.one);
        GameObject startCard = UIPanel("Selection Card", start.transform, new Color(Panel.r, Panel.g, Panel.b, 0.98f),
            new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.86f));
        Text title = UIText("Title", startCard.transform, font, "Choose your interviewer", 48, Cream, TextAnchor.MiddleCenter,
            new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.92f));
        title.fontStyle = FontStyle.Bold;
        UIText("Subtitle", startCard.transform, font,
            "Practice the same 12-question interview with a different communication style.", 24, Muted, TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.61f), new Vector2(0.9f, 0.75f));

        Button warmButton = UIButton("Warm Persona", startCard.transform, font, "1  WARM & ENCOURAGING\nSupportive prompts and reassuring pacing", Warm,
            new Vector2(0.08f, 0.40f), new Vector2(0.36f, 0.61f));
        Button neutralButton = UIButton("Neutral Persona", startCard.transform, font, "2  NEUTRAL & PROFESSIONAL\nMeasured prompts and balanced pacing", Neutral,
            new Vector2(0.36f, 0.40f), new Vector2(0.64f, 0.61f));
        Button sternButton = UIButton("Stern Persona", startCard.transform, font, "3  STERN & CHALLENGING\nDirect prompts and firmer delivery", Stern,
            new Vector2(0.64f, 0.40f), new Vector2(0.92f, 0.61f));
        string controlsHint = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
            ? "QUEST CONTROLS  •  A / trigger: Warm  •  B: Neutral  •  X or Y: Stern"
            : "Click a persona or press 1, 2, or 3  •  Headphones recommended";
        UIText("Controls Hint", startCard.transform, font, controlsHint, 20, Muted,
            TextAnchor.MiddleCenter, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.32f));
        UIText("Fallback Notice", startCard.transform, font,
            "Prototype note: persona voice, pacing, gestures, and colour change now; the supplied T1 Avaturn body is reused until distinct facial-rig exports arrive.",
            17, new Color(Muted.r, Muted.g, Muted.b, 0.78f), TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.20f));

        GameObject interview = new GameObject("Interview HUD", typeof(RectTransform));
        interview.transform.SetParent(canvasGo.transform, false);
        Stretch(interview.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject card = UIPanel("Question Card", interview.transform, new Color(Panel.r, Panel.g, Panel.b, 0.97f),
            new Vector2(0.63f, 0.12f), new Vector2(0.965f, 0.89f));
        Text personaTitle = UIText("Persona", card.transform, font, "INTERVIEWER", 27, Neutral, TextAnchor.MiddleLeft,
            new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f));
        personaTitle.fontStyle = FontStyle.Bold;
        Text progress = UIText("Progress", card.transform, font, "Question — / 12", 20, Muted, TextAnchor.MiddleLeft,
            new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.82f));
        Text question = UIText("Question", card.transform, font, "Select a persona to begin.", 30, Cream, TextAnchor.MiddleLeft,
            new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.73f));
        question.resizeTextForBestFit = true;
        question.resizeTextMinSize = 20;
        question.resizeTextMaxSize = 30;
        Text status = UIText("Status", card.transform, font, "READY", 17, Muted, TextAnchor.MiddleLeft,
            new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.35f));
        Button next = UIButton("Next Question", card.transform, font, "NEXT QUESTION", Neutral,
            new Vector2(0.08f, 0.11f), new Vector2(0.92f, 0.25f));

        GameObject complete = UIPanel("Completion", canvasGo.transform, new Color(Navy.r, Navy.g, Navy.b, 0.84f), Vector2.zero, Vector2.one);
        GameObject completeCard = UIPanel("Completion Card", complete.transform, new Color(Panel.r, Panel.g, Panel.b, 0.99f),
            new Vector2(0.29f, 0.24f), new Vector2(0.71f, 0.76f));
        Text completeTitle = UIText("Complete Title", completeCard.transform, font, "Rehearsal complete", 44, Cream, TextAnchor.MiddleCenter,
            new Vector2(0.08f, 0.69f), new Vector2(0.92f, 0.9f));
        completeTitle.fontStyle = FontStyle.Bold;
        Text summary = UIText("Summary", completeCard.transform, font, "You completed the interview.", 23, Muted, TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.68f));
        Button restart = UIButton("Restart", completeCard.transform, font, "RESTART", Neutral,
            new Vector2(0.1f, 0.17f), new Vector2(0.47f, 0.36f));
        Button menu = UIButton("Change Persona", completeCard.transform, font, "CHANGE PERSONA", Warm,
            new Vector2(0.53f, 0.17f), new Vector2(0.9f, 0.36f));

        PlaybackUI ui = canvasGo.AddComponent<PlaybackUI>();
        ui.playbackController = controller;
        ui.personaManager = personas;
        ui.personaTitleText = personaTitle;
        ui.questionProgressText = progress;
        ui.questionContentText = question;
        ui.statusText = status;
        ui.nextButton = next;
        ui.warmButton = warmButton;
        ui.neutralButton = neutralButton;
        ui.sternButton = sternButton;
        ui.restartButton = restart;
        ui.menuButton = menu;
        ui.startPanel = start;
        ui.interviewPanel = interview;
        ui.completionPanel = complete;
        ui.completionSummaryText = summary;

        interview.SetActive(false);
        complete.SetActive(false);
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private static GameObject UIPanel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        Stretch(go.GetComponent<RectTransform>(), min, max);
        return go;
    }

    private static Text UIText(string name, Transform parent, Font font, string value, int size, Color color,
        TextAnchor alignment, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(go.GetComponent<RectTransform>(), min, max);
        return text;
    }

    private static Button UIButton(string name, Transform parent, Font font, string label, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = UIPanel(name, parent, color, min, max);
        Button button = go.AddComponent<Button>();
        ColorBlock block = button.colors;
        block.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        block.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        block.selectedColor = block.highlightedColor;
        button.colors = block;
        Text text = UIText("Label", go.transform, font, label, 20, Navy, TextAnchor.MiddleCenter,
            new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
        text.fontStyle = FontStyle.Bold;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = 22;
        return button;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        return Primitive(name, type, position, scale, Material(name, color), null, false);
    }

    private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color,
        Transform parent, bool local)
    {
        return Primitive(name, type, position, scale, Material(name, color), parent, local);
    }

    private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material,
        Transform parent = null, bool local = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        if (local) go.transform.localPosition = position; else go.transform.position = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        return go;
    }

    private static Material Material(string name, Color color)
    {
        string safeName = string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        string path = $"{MaterialDirectory}/{safeName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = safeName };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Transform FindDescendant(Transform root, string target)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(child.name, target, StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }

    private static Color Html(string value)
    {
        return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.white;
    }

    [MenuItem("V-STIPA/Build WebGL")]
    public static void BuildWebGL()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        SetupMainScene();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        string output = GetCommandLineValue("-vstipaOutput") ??
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../local-build/webgl"));
        Directory.CreateDirectory(output);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        ReportBuild(report, output);
    }

    [MenuItem("V-STIPA/Validate Phase 3 + 4")]
    public static void ValidatePhase3And4()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var failures = new List<string>();

        PersonaManager manager = UnityEngine.Object.FindAnyObjectByType<PersonaManager>();
        QuestionPlaybackController controller = UnityEngine.Object.FindAnyObjectByType<QuestionPlaybackController>();
        PlaybackUI ui = UnityEngine.Object.FindAnyObjectByType<PlaybackUI>();
        uLipSync.uLipSync analyzer = UnityEngine.Object.FindAnyObjectByType<uLipSync.uLipSync>();

        if (manager == null || manager.personas == null || manager.personas.Length != 3)
            failures.Add("PersonaManager must contain exactly three persona slots.");
        if (controller == null || controller.audioSource == null || controller.loadOnStart)
            failures.Add("Playback controller is not configured for menu-driven audio playback.");
        if (analyzer == null || analyzer.profile == null)
            failures.Add("uLipSync analyzer/profile is missing.");
        if (ui == null || ui.startPanel == null || ui.interviewPanel == null || ui.completionPanel == null ||
            ui.warmButton == null || ui.neutralButton == null || ui.sternButton == null)
            failures.Add("Persona selection, interview, or completion UI references are incomplete.");
        if (GameObject.Find("Slim Executive Interview Desk") == null ||
            GameObject.Find("Interviewer Executive Chair") == null || Camera.main == null)
            failures.Add("Interview room or main camera is missing.");

        int realVisemeAvatars = 0;
        var sourceIds = new HashSet<string>();
        if (manager?.personas != null)
        {
            foreach (PersonaManager.PersonaSlot slot in manager.personas)
            {
                if (slot == null || slot.avatarRoot == null || slot.gestureController == null || slot.lipSync == null)
                {
                    failures.Add("A persona is missing its avatar, gesture controller, or lip-sync component.");
                    continue;
                }

                if (slot.lipSync.mouthProxy == null)
                    failures.Add($"{slot.persona} has neither a verified facial rig nor a mouth fallback assigned.");

                foreach (SkinnedMeshRenderer renderer in slot.avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0) realVisemeAvatars++;
                    Debug.Log($"[V-STIPA Avatar Audit] {slot.persona}/{renderer.name}: enabled={renderer.enabled}, " +
                        $"vertices={renderer.sharedMesh?.vertexCount ?? 0}, bounds={renderer.localBounds}, " +
                        $"materials={renderer.sharedMaterials.Length}.");
                    foreach (Material rendererMaterial in renderer.sharedMaterials)
                        Debug.Log($"[V-STIPA Material Audit] {slot.persona}/{renderer.name}: material={rendererMaterial?.name}, " +
                            $"shader={rendererMaterial?.shader?.name}, supported={rendererMaterial?.shader?.isSupported}, " +
                            $"queue={rendererMaterial?.renderQueue}, passes={rendererMaterial?.passCount}.");
                }

                string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(slot.avatarRoot);
                if (!string.IsNullOrEmpty(source)) sourceIds.Add(source);

                string manifestPath = Path.Combine(Application.streamingAssetsPath, "questions", slot.persona, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    failures.Add($"Missing {slot.persona} manifest.");
                    continue;
                }

                PersonaManifestData manifest = JsonUtility.FromJson<PersonaManifestData>(File.ReadAllText(manifestPath));
                if (manifest?.questions == null || manifest.questions.Count != 12 || manifest.total_questions != 12)
                    failures.Add($"{slot.persona} manifest must contain 12 questions.");
                else
                    foreach (QuestionItemData item in manifest.questions)
                        if (!File.Exists(Path.Combine(Application.streamingAssetsPath, "questions", slot.persona, item.audio_file)))
                            failures.Add($"Missing audio for {slot.persona} Q{item.id:D2}: {item.audio_file}");
            }
        }

        if (failures.Count > 0)
            throw new BuildFailedException("Phase 3 + 4 validation failed:\n- " + string.Join("\n- ", failures));

        Debug.Log($"[V-STIPA Validation] PASS: room/UI, 3 persona configurations, 36 questions, audio references, gestures, and lip-sync fallback are wired. " +
            $"Capability disclosure: {sourceIds.Count} distinct avatar source file(s); {realVisemeAvatars} avatar instance(s) expose facial blendshapes. " +
            "The full three-distinct-avatar/real-viseme Phase 3 gate remains pending until suitable exports are supplied.");
    }

    [MenuItem("V-STIPA/Build Android")]
    public static void BuildAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        ConfigureQuestProject();
        SetupMainScene();
        ValidateQuestConfiguration();

        string output = GetCommandLineValue("-vstipaOutput") ??
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../local-build/android/V-STIPA-Quest.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        ReportBuild(report, output);
    }

    [MenuItem("V-STIPA/Configure Quest OpenXR")]
    public static void ConfigureQuestProject()
    {
        Directory.CreateDirectory(XrSettingsDirectory);
        AssetDatabase.Refresh();

        XRGeneralSettingsPerBuildTarget perTarget =
            AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XrGeneralSettingsPath);
        if (perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, XrGeneralSettingsPath);
        }
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);

        if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        XRGeneralSettings general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        general.InitManagerOnStart = true;
        XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        OpenXRLoader loader = AssetDatabase.LoadAssetAtPath<OpenXRLoader>(OpenXrLoaderPath);
        if (loader == null)
        {
            loader = ScriptableObject.CreateInstance<OpenXRLoader>();
            AssetDatabase.CreateAsset(loader, OpenXrLoaderPath);
        }
        if (!manager.TrySetLoaders(new List<XRLoader> { loader }))
            throw new BuildFailedException("Could not assign the Android OpenXR loader.");

        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        EnableOpenXrFeature("com.unity.openxr.feature.metaquest", "Meta Quest Support");
        EnableOpenXrFeature("com.unity.openxr.feature.input.oculustouch", "Oculus Touch Controller Profile");
        EnableOpenXrFeature("com.unity.openxr.feature.input.metaquestplus", "Meta Quest Touch Plus Controller Profile");
        EnableOpenXrFeature("com.unity.openxr.feature.compositionlayers", "Composition Layers Support");

        var metaQuestFeature = FeatureHelpers.GetFeatureWithIdForBuildTarget(
            BuildTargetGroup.Android, "com.unity.openxr.feature.metaquest");
        var serializedMetaQuest = new SerializedObject(metaQuestFeature);
        SerializedProperty targetDevices = serializedMetaQuest.FindProperty("targetDevices");
        for (int i = 0; targetDevices != null && i < targetDevices.arraySize; i++)
        {
            SerializedProperty device = targetDevices.GetArrayElementAtIndex(i);
            string manifestName = device.FindPropertyRelative("manifestName").stringValue;
            device.FindPropertyRelative("enabled").boolValue =
                manifestName == "eureka" || manifestName == "quest3s";
        }
        serializedMetaQuest.FindProperty("forceRemoveInternetPermission").boolValue = true;
        serializedMetaQuest.FindProperty("optimizeBufferDiscards").boolValue = false;
        serializedMetaQuest.ApplyModifiedPropertiesWithoutUndo();

        OpenXRSettings androidOpenXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        androidOpenXrSettings.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
        EditorUtility.SetDirty(androidOpenXrSettings);

        PlayerSettings.companyName = "Mirza Ahsan";
        PlayerSettings.productName = "V-STIPA Interview Simulator";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mirzaahsan.vstipa");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.MTRendering = true;

        EditorUtility.SetDirty(perTarget);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(loader);
        AssetDatabase.SaveAssets();
        Debug.Log("[QuestConfig] Android OpenXR configured for Quest: ARM64, IL2CPP, API 29+, OpenGLES3, Meta Quest + Touch profiles.");
    }

    [MenuItem("V-STIPA/Validate Quest Configuration")]
    public static void ValidateQuestConfiguration()
    {
        var failures = new List<string>();
        XRGeneralSettings general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (general == null || !general.InitManagerOnStart || general.Manager == null)
            failures.Add("Android XR General Settings are missing or do not initialize on startup.");
        else
        {
            bool hasOpenXrLoader = false;
            foreach (XRLoader activeLoader in general.Manager.activeLoaders)
                if (activeLoader is OpenXRLoader) hasOpenXrLoader = true;
            if (!hasOpenXrLoader) failures.Add("Android OpenXR loader is not assigned.");
        }

        ValidateOpenXrFeature("com.unity.openxr.feature.metaquest", "Meta Quest Support", failures);
        ValidateOpenXrFeature("com.unity.openxr.feature.input.oculustouch", "Oculus Touch", failures);
        ValidateOpenXrFeature("com.unity.openxr.feature.input.metaquestplus", "Quest Touch Plus", failures);
        ValidateOpenXrFeature("com.unity.openxr.feature.compositionlayers", "Composition Layers Support", failures);

        if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            failures.Add("Android target architecture must be ARM64 only.");
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            failures.Add("Android scripting backend must be IL2CPP.");
        if (UnityEngine.Object.FindAnyObjectByType<QuestRuntimeAdapter>() == null)
            failures.Add("QuestRuntimeAdapter is missing from MainScene.");
        if (Camera.main == null || Camera.main.GetComponent<TrackedPoseDriver>() == null)
            failures.Add("The main camera is not configured for headset pose tracking.");

        if (failures.Count > 0)
            throw new BuildFailedException("Quest validation failed:\n- " + string.Join("\n- ", failures));
        Debug.Log("[QuestConfig] VALIDATION PASS: OpenXR startup, Meta Quest support, Touch/Touch Plus input, tracked camera, ARM64 and IL2CPP are configured.");
    }

    private static void EnableOpenXrFeature(string featureId, string displayName)
    {
        var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
        if (feature == null) throw new BuildFailedException($"OpenXR feature not found: {displayName} ({featureId}).");
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }

    private static void ValidateOpenXrFeature(string featureId, string displayName, List<string> failures)
    {
        var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
        if (feature == null || !feature.enabled) failures.Add($"{displayName} OpenXR feature is not enabled.");
    }

    private static void ReportBuild(BuildReport report, string output)
    {
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"V-STIPA build failed with {summary.totalErrors} errors.");
        Debug.Log($"[BuildScript] Build succeeded: {summary.totalSize} bytes at {output}");
    }

    private static string GetCommandLineValue(string key)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], key, StringComparison.Ordinal)) return args[i + 1];
        return null;
    }
}
