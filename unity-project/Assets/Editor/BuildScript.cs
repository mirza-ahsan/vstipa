using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;

public static class BuildScript
{
    [MenuItem("V-STIPA/Setup Main Scene")]
    public static void SetupMainScene()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        // 1. Create PlaybackController
        GameObject playbackGo = new GameObject("PlaybackController");
        QuestionPlaybackController controller = playbackGo.AddComponent<QuestionPlaybackController>();
        controller.selectedPersona = "warm";
        AudioSource audioSource = playbackGo.AddComponent<AudioSource>();
        controller.audioSource = audioSource;

        // 2. Create UI Canvas
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.position = new Vector3(0, 1.5f, 2.0f);
        canvasRt.sizeDelta = new Vector2(800, 500);
        canvasRt.localScale = new Vector3(0.003f, 0.003f, 0.003f);

        // Background Panel
        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        // Persona Title Text
        GameObject personaTextGo = new GameObject("PersonaTitleText");
        personaTextGo.transform.SetParent(panelGo.transform, false);
        Text personaTitleText = personaTextGo.AddComponent<Text>();
        personaTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        personaTitleText.fontSize = 28;
        personaTitleText.alignment = TextAnchor.MiddleCenter;
        personaTitleText.color = new Color(0.3f, 0.8f, 1.0f);
        personaTitleText.text = "Warm & Encouraging Interviewer";
        RectTransform personaRt = personaTextGo.GetComponent<RectTransform>();
        personaRt.anchoredPosition = new Vector3(0, 180, 0);
        personaRt.sizeDelta = new Vector2(700, 50);

        // Question Progress Text
        GameObject progressTextGo = new GameObject("QuestionProgressText");
        progressTextGo.transform.SetParent(panelGo.transform, false);
        Text questionProgressText = progressTextGo.AddComponent<Text>();
        questionProgressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        questionProgressText.fontSize = 22;
        questionProgressText.alignment = TextAnchor.MiddleCenter;
        questionProgressText.color = Color.white;
        questionProgressText.text = "Press Next Question to Begin";
        RectTransform progressRt = progressTextGo.GetComponent<RectTransform>();
        progressRt.anchoredPosition = new Vector3(0, 120, 0);
        progressRt.sizeDelta = new Vector2(700, 40);

        // Question Content Text
        GameObject contentTextGo = new GameObject("QuestionContentText");
        contentTextGo.transform.SetParent(panelGo.transform, false);
        Text questionContentText = contentTextGo.AddComponent<Text>();
        questionContentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        questionContentText.fontSize = 24;
        questionContentText.alignment = TextAnchor.MiddleCenter;
        questionContentText.color = Color.yellow;
        questionContentText.text = "\"Welcome to V-STIPA VR Training Demo.\"";
        RectTransform contentRt = contentTextGo.GetComponent<RectTransform>();
        contentRt.anchoredPosition = new Vector3(0, 20, 0);
        contentRt.sizeDelta = new Vector2(720, 140);

        // Status Text
        GameObject statusTextGo = new GameObject("StatusText");
        statusTextGo.transform.SetParent(panelGo.transform, false);
        Text statusText = statusTextGo.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 18;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.gray;
        statusText.text = "Offline Playback Ready (Airplane Mode Tested)";
        RectTransform statusRt = statusTextGo.GetComponent<RectTransform>();
        statusRt.anchoredPosition = new Vector3(0, -70, 0);
        statusRt.sizeDelta = new Vector2(700, 30);

        // Next Question Button
        GameObject buttonGo = new GameObject("NextButton");
        buttonGo.transform.SetParent(panelGo.transform, false);
        Image btnImg = buttonGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.9f);
        Button btn = buttonGo.AddComponent<Button>();
        RectTransform btnRt = buttonGo.GetComponent<RectTransform>();
        btnRt.anchoredPosition = new Vector3(0, -140, 0);
        btnRt.sizeDelta = new Vector2(260, 60);

        GameObject btnTextGo = new GameObject("BtnText");
        btnTextGo.transform.SetParent(buttonGo.transform, false);
        Text btnText = btnTextGo.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 22;
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
        Debug.Log("[BuildScript] MainScene.unity configured and saved successfully.");
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
