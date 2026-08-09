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

        // 1. Position Main Camera for comfortable VR viewing (1.35m height)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 1.35f, 0);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.06f, 0.08f, 0.14f);
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

        // 4. Create WorldSpace UI Canvas Calibrated for Meta Quest VR (1.2m Distance, 1.0m Width)
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.position = new Vector3(0, 1.35f, 1.2f); // 1.2m directly in front of camera
        canvasRt.sizeDelta = new Vector2(1000, 700);
        canvasRt.localScale = new Vector3(0.001f, 0.001f, 0.001f); // 1.0m x 0.7m physical size in VR

        // Background Panel
        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.12f, 0.20f, 0.96f);
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
        personaTitleText.fontSize = 44;
        personaTitleText.alignment = TextAnchor.MiddleCenter;
        personaTitleText.color = new Color(0.35f, 0.85f, 1.0f);
        personaTitleText.text = "Warm & Encouraging Interviewer";
        personaTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        personaTitleText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform personaRt = personaTextGo.GetComponent<RectTransform>();
        personaRt.anchoredPosition = new Vector3(0, 260, 0);
        personaRt.sizeDelta = new Vector2(900, 60);

        // Question Progress Text
        GameObject progressTextGo = new GameObject("QuestionProgressText");
        progressTextGo.transform.SetParent(panelGo.transform, false);
        Text questionProgressText = progressTextGo.AddComponent<Text>();
        questionProgressText.font = legacyFont;
        questionProgressText.fontSize = 32;
        questionProgressText.alignment = TextAnchor.MiddleCenter;
        questionProgressText.color = Color.white;
        questionProgressText.text = "Press Any Quest Controller Button or Trigger to Begin";
        questionProgressText.horizontalOverflow = HorizontalWrapMode.Wrap;
        questionProgressText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform progressRt = progressTextGo.GetComponent<RectTransform>();
        progressRt.anchoredPosition = new Vector3(0, 190, 0);
        progressRt.sizeDelta = new Vector2(900, 50);

        // Question Content Text
        GameObject contentTextGo = new GameObject("QuestionContentText");
        contentTextGo.transform.SetParent(panelGo.transform, false);
        Text questionContentText = contentTextGo.AddComponent<Text>();
        questionContentText.font = legacyFont;
        questionContentText.fontSize = 36;
        questionContentText.alignment = TextAnchor.MiddleCenter;
        questionContentText.color = new Color(1.0f, 0.95f, 0.70f);
        questionContentText.text = "\"Welcome to V-STIPA VR Training Simulation.\"\n(Press Quest Controller Trigger or Button A/B/X/Y to advance questions)";
        questionContentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        questionContentText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform contentRt = contentTextGo.GetComponent<RectTransform>();
        contentRt.anchoredPosition = new Vector3(0, 30, 0);
        contentRt.sizeDelta = new Vector2(920, 220);

        // Status Text
        GameObject statusTextGo = new GameObject("StatusText");
        statusTextGo.transform.SetParent(panelGo.transform, false);
        Text statusText = statusTextGo.AddComponent<Text>();
        statusText.font = legacyFont;
        statusText.fontSize = 26;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.lightGray;
        statusText.text = "Offline Playback Mode (Airplane Mode Tested)";
        statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform statusRt = statusTextGo.GetComponent<RectTransform>();
        statusRt.anchoredPosition = new Vector3(0, -140, 0);
        statusRt.sizeDelta = new Vector2(900, 40);

        // Next Question Button
        GameObject buttonGo = new GameObject("NextButton");
        buttonGo.transform.SetParent(panelGo.transform, false);
        Image btnImg = buttonGo.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.58f, 0.95f);
        Button btn = buttonGo.AddComponent<Button>();
        RectTransform btnRt = buttonGo.GetComponent<RectTransform>();
        btnRt.anchoredPosition = new Vector3(0, -230, 0);
        btnRt.sizeDelta = new Vector2(380, 85);

        GameObject btnTextGo = new GameObject("BtnText");
        btnTextGo.transform.SetParent(buttonGo.transform, false);
        Text btnText = btnTextGo.AddComponent<Text>();
        btnText.font = legacyFont;
        btnText.fontSize = 32;
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
        Debug.Log("[BuildScript] Calibrated Meta Quest VR MainScene.unity configured and saved successfully.");
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
