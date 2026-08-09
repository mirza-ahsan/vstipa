using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BuildScript
{
    [MenuItem("V-STIPA/Setup Main Scene")]
    public static void SetupMainScene()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        // 1. Position Main Camera for VR headset viewing distance
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 1.6f, 0);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.04f, 0.06f, 0.10f);
        }

        // 2. Create PlaybackController
        GameObject playbackGo = new GameObject("PlaybackController");
        QuestionPlaybackController controller = playbackGo.AddComponent<QuestionPlaybackController>();
        controller.selectedPersona = "warm";
        AudioSource audioSource = playbackGo.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Crisp 2D UI stereo sound
        controller.audioSource = audioSource;

        // 3. Create EventSystem
        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();

        // 4. Create UI Canvas with Ultra-High DPI Scaling for Razor-Sharp VR Rendering
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        scaler.referencePixelsPerUnit = 100f;

        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.position = new Vector3(0, 1.6f, 1.8f);
        canvasRt.sizeDelta = new Vector2(3200, 2000);
        canvasRt.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

        // Background Panel
        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.10f, 0.16f, 0.96f);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Persona Title Text
        GameObject personaTextGo = new GameObject("PersonaTitleText");
        personaTextGo.transform.SetParent(panelGo.transform, false);
        Text personaTitleText = personaTextGo.AddComponent<Text>();
        personaTitleText.font = legacyFont;
        personaTitleText.fontSize = 110;
        personaTitleText.alignment = TextAnchor.MiddleCenter;
        personaTitleText.color = new Color(0.3f, 0.85f, 1.0f);
        personaTitleText.text = "Warm & Encouraging Interviewer";
        RectTransform personaRt = personaTextGo.GetComponent<RectTransform>();
        personaRt.anchoredPosition = new Vector3(0, 750, 0);
        personaRt.sizeDelta = new Vector2(3000, 200);

        // Question Progress Text
        GameObject progressTextGo = new GameObject("QuestionProgressText");
        progressTextGo.transform.SetParent(panelGo.transform, false);
        Text questionProgressText = progressTextGo.AddComponent<Text>();
        questionProgressText.font = legacyFont;
        questionProgressText.fontSize = 85;
        questionProgressText.alignment = TextAnchor.MiddleCenter;
        questionProgressText.color = Color.white;
        questionProgressText.text = "Press Any Quest Controller Button / Trigger to Start";
        RectTransform progressRt = progressTextGo.GetComponent<RectTransform>();
        progressRt.anchoredPosition = new Vector3(0, 520, 0);
        progressRt.sizeDelta = new Vector2(3000, 160);

        // Question Content Text
        GameObject contentTextGo = new GameObject("QuestionContentText");
        contentTextGo.transform.SetParent(panelGo.transform, false);
        Text questionContentText = contentTextGo.AddComponent<Text>();
        questionContentText.font = legacyFont;
        questionContentText.fontSize = 95;
        questionContentText.alignment = TextAnchor.MiddleCenter;
        questionContentText.color = new Color(1.0f, 0.95f, 0.75f);
        questionContentText.text = "\"Welcome to V-STIPA VR Training Simulation.\"\n(Press Quest Trigger or Button A/B/X/Y to advance questions)";
        RectTransform contentRt = contentTextGo.GetComponent<RectTransform>();
        contentRt.anchoredPosition = new Vector3(0, 80, 0);
        contentRt.sizeDelta = new Vector2(3000, 600);

        // Status Text
        GameObject statusTextGo = new GameObject("StatusText");
        statusTextGo.transform.SetParent(panelGo.transform, false);
        Text statusText = statusTextGo.AddComponent<Text>();
        statusText.font = legacyFont;
        statusText.fontSize = 70;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.lightGray;
        statusText.text = "Offline Playback Mode (Airplane Mode Tested)";
        RectTransform statusRt = statusTextGo.GetComponent<RectTransform>();
        statusRt.anchoredPosition = new Vector3(0, -360, 0);
        statusRt.sizeDelta = new Vector2(3000, 120);

        // Next Question Button
        GameObject buttonGo = new GameObject("NextButton");
        buttonGo.transform.SetParent(panelGo.transform, false);
        Image btnImg = buttonGo.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.58f, 0.95f);
        Button btn = buttonGo.AddComponent<Button>();
        RectTransform btnRt = buttonGo.GetComponent<RectTransform>();
        btnRt.anchoredPosition = new Vector3(0, -620, 0);
        btnRt.sizeDelta = new Vector2(1000, 220);

        GameObject btnTextGo = new GameObject("BtnText");
        btnTextGo.transform.SetParent(buttonGo.transform, false);
        Text btnText = btnTextGo.AddComponent<Text>();
        btnText.font = legacyFont;
        btnText.fontSize = 85;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "Next Question";
        RectTransform btnTextRt = btnTextGo.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.sizeDelta = Vector2.zero;

        // Attach PlaybackUI component
        PlaybackUI ui = canvasGo.AddComponent<PlaybackUI>();
        ui.playbackController = controller;
        ui.personaTitleText = personaTitleText;
        ui.questionProgressText = questionProgressText;
        ui.questionContentText = questionContentText;
        ui.statusText = statusText;
        ui.nextButton = btn;

        string scenePath = "Assets/Scenes/MainScene.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[BuildScript] High-DPI Crisp VR MainScene.unity configured and saved successfully.");
    }

    public static void BuildAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        SetupMainScene();

        string[] scenes = new string[] { "Assets/Scenes/MainScene.unity" };

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "/home/aztrek/Projects/vstipa/build.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed: " + summary.totalErrors + " errors");
        }
    }
}
