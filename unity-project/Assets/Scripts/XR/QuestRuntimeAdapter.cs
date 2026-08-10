using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Keeps the baked interview flow usable on a standalone Quest without adding a
/// network or pointer dependency. The right controller advances the interview;
/// face buttons provide deterministic menu choices.
/// </summary>
public class QuestRuntimeAdapter : MonoBehaviour
{
    public Camera headsetCamera;
    public Canvas interfaceCanvas;
    public PlaybackUI playbackUI;

    private InputDevice leftController;
    private InputDevice rightController;
    private bool previousLeftPrimary;
    private bool previousLeftSecondary;
    private bool previousRightPrimary;
    private bool previousRightSecondary;
    private bool previousRightTrigger;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.targetFrameRate = 72;
        QualitySettings.vSyncCount = 0;
        ConfigureWorldSpaceInterface();
        StartCoroutine(LogRuntimeStatus());
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        RefreshControllers();

        bool leftPrimary = ReadButton(leftController, CommonUsages.primaryButton);
        bool leftSecondary = ReadButton(leftController, CommonUsages.secondaryButton);
        bool rightPrimary = ReadButton(rightController, CommonUsages.primaryButton);
        bool rightSecondary = ReadButton(rightController, CommonUsages.secondaryButton);
        bool rightTrigger = ReadButton(rightController, CommonUsages.triggerButton);

        bool leftPrimaryDown = leftPrimary && !previousLeftPrimary;
        bool leftSecondaryDown = leftSecondary && !previousLeftSecondary;
        bool rightPrimaryDown = rightPrimary && !previousRightPrimary;
        bool rightSecondaryDown = rightSecondary && !previousRightSecondary;
        bool rightTriggerDown = rightTrigger && !previousRightTrigger;

        previousLeftPrimary = leftPrimary;
        previousLeftSecondary = leftSecondary;
        previousRightPrimary = rightPrimary;
        previousRightSecondary = rightSecondary;
        previousRightTrigger = rightTrigger;

        if (playbackUI == null) return;

        if (playbackUI.startPanel != null && playbackUI.startPanel.activeSelf)
        {
            if (rightPrimaryDown || rightTriggerDown) playbackUI.SelectPersona("warm");
            else if (rightSecondaryDown) playbackUI.SelectPersona("neutral");
            else if (leftPrimaryDown || leftSecondaryDown) playbackUI.SelectPersona("stern");
            return;
        }

        if (playbackUI.completionPanel != null && playbackUI.completionPanel.activeSelf)
        {
            if (rightPrimaryDown || rightTriggerDown) playbackUI.RestartInterview();
            else if (rightSecondaryDown) playbackUI.ShowStartScreen();
            return;
        }

        if (playbackUI.interviewPanel != null && playbackUI.interviewPanel.activeSelf &&
            (rightPrimaryDown || rightTriggerDown))
        {
            playbackUI.OnNextButtonClicked();
        }
#endif
    }

    private void ConfigureWorldSpaceInterface()
    {
        if (headsetCamera == null || interfaceCanvas == null)
        {
            Debug.LogError("[QuestRuntime] Camera or interface canvas is missing.");
            return;
        }

        interfaceCanvas.renderMode = RenderMode.WorldSpace;
        interfaceCanvas.worldCamera = headsetCamera;
        RectTransform rect = interfaceCanvas.GetComponent<RectTransform>();
        rect.SetParent(headsetCamera.transform, false);
        rect.localPosition = new Vector3(0f, 0f, 2.25f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.00155f;
        rect.sizeDelta = new Vector2(1920f, 1080f);

        CanvasScaler scaler = interfaceCanvas.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.dynamicPixelsPerUnit = 12f;
        Debug.Log("[QuestRuntime] World-space headset UI configured. Controls: A/trigger=Warm or Next, B=Neutral/Menu, X/Y=Stern.");
    }

    private void RefreshControllers()
    {
        if (!leftController.isValid) leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!rightController.isValid) rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
    {
        return device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;
    }

    private IEnumerator LogRuntimeStatus()
    {
        yield return new WaitForSecondsRealtime(1f);
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        bool running = displays.Exists(display => display != null && display.running);
        Debug.Log($"[QuestRuntime] OpenXR display running={running}; displays={displays.Count}; device={SystemInfo.deviceModel}; graphics={SystemInfo.graphicsDeviceType}.");
        if (!running) Debug.LogError("[QuestRuntime] No running XR display subsystem was found.");
    }
}
