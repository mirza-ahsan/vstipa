using UnityEngine;
using UnityEngine.UI;

public class PlaybackUI : MonoBehaviour
{
    [Header("Controller Reference")]
    public QuestionPlaybackController playbackController;

    [Header("UI Text Components")]
    public Text personaTitleText;
    public Text questionProgressText;
    public Text questionContentText;
    public Text statusText;
    public Text startStatusText;
    public InputField targetRoleInput;

    [Header("Buttons")]
    public Button nextButton;
    public Button warmButton;
    public Button sternButton;
    public Button neutralButton;
    public Button restartButton;
    public Button menuButton;

    [Header("Experience Panels")]
    public GameObject startPanel;
    public GameObject interviewPanel;
    public GameObject completionPanel;
    public Text completionSummaryText;
    public PersonaManager personaManager;

    [Header("Input Safety")]
    [Tooltip("Minimum time between accepted Next actions. Prevents mouse, keyboard, and Quest double-presses from skipping a question.")]
    public float advanceDebounceSeconds = 1.5f;

    private float nextAdvanceAllowedAt = float.NegativeInfinity;

    private void Start()
    {
        if (playbackController == null)
        {
            playbackController = FindAnyObjectByType<QuestionPlaybackController>();
        }

        if (playbackController != null)
        {
            playbackController.OnQuestionChanged += HandleQuestionChanged;
            playbackController.OnPlaybackFinished += HandlePlaybackFinished;
            playbackController.OnStatusChanged += HandleStatusChanged;
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        warmButton?.onClick.AddListener(() => SelectPersona("warm"));
        sternButton?.onClick.AddListener(() => SelectPersona("stern"));
        neutralButton?.onClick.AddListener(() => SelectPersona("neutral"));
        restartButton?.onClick.AddListener(RestartInterview);
        menuButton?.onClick.AddListener(ShowStartScreen);

        ShowStartScreen();
    }

    private void Update()
    {
        // Detect VR controller inputs (A, B, X, Y, Triggers, Touchpad) or Keyboard (Space, Enter, Click)
        // Mouse clicks are handled exclusively by Button.onClick. Including the
        // legacy Fire1 mapping here would process the same physical click twice.
        bool vrInputDetected = Input.GetButtonDown("Submit") ||
                               Input.GetKeyDown(KeyCode.Space) ||
                               Input.GetKeyDown(KeyCode.Return) ||
                               Input.GetKeyDown(KeyCode.JoystickButton0) ||
                               Input.GetKeyDown(KeyCode.JoystickButton1) ||
                               Input.GetKeyDown(KeyCode.JoystickButton2) ||
                               Input.GetKeyDown(KeyCode.JoystickButton3) ||
                               Input.GetKeyDown(KeyCode.JoystickButton14) ||
                               Input.GetKeyDown(KeyCode.JoystickButton15);

        if (startPanel != null && startPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectPersona("warm");
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectPersona("neutral");
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectPersona("stern");
            return;
        }

        if (vrInputDetected && interviewPanel != null && interviewPanel.activeSelf &&
            playbackController?.currentManifest != null)
        {
            OnNextButtonClicked();
        }
    }

    public void SelectPersona(string persona)
    {
        string role = targetRoleInput?.text?.Trim();
        if (string.IsNullOrEmpty(role))
        {
            if (startStatusText != null) startStatusText.text = "Enter the role or position you want to practise for.";
            return;
        }
        if (personaManager == null || !personaManager.SelectPersona(persona, role)) return;
        PersonaManager.PersonaSlot slot = personaManager.GetActiveSlot();
        if (slot != null)
        {
            if (personaTitleText != null) personaTitleText.color = slot.accentColor;
            if (nextButton != null && nextButton.image != null) nextButton.image.color = slot.accentColor;
        }
        startPanel?.SetActive(false);
        completionPanel?.SetActive(false);
        interviewPanel?.SetActive(true);
        if (statusText != null) statusText.text = "Preparing interview...";
    }

    public void ShowStartScreen()
    {
        personaManager?.ReturnToSelection();
        startPanel?.SetActive(true);
        interviewPanel?.SetActive(false);
        completionPanel?.SetActive(false);
        if (startStatusText != null) startStatusText.text = "READY  •  ENTER A ROLE TO BEGIN";
    }

    public void RestartInterview()
    {
        completionPanel?.SetActive(false);
        interviewPanel?.SetActive(true);
        playbackController?.RestartPlayback();
    }

    public void OnNextButtonClicked()
    {
        TryAdvance(Time.unscaledTime);
    }

    public bool TryAdvance(float timestamp)
    {
        if (playbackController == null)
        {
            return false;
        }
        if (timestamp < nextAdvanceAllowedAt)
        {
            Debug.Log("[PlaybackUI] Ignored duplicate Next input during debounce window.");
            return false;
        }

        nextAdvanceAllowedAt = timestamp + Mathf.Max(0.1f, advanceDebounceSeconds);
        playbackController.AdvanceToNextQuestion();
        return true;
    }

    private void HandleQuestionChanged(QuestionItemData item)
    {
        if (personaTitleText != null)
        {
            personaTitleText.text = playbackController.currentManifest?.persona_name ?? playbackController.selectedPersona;
        }

        if (questionProgressText != null)
        {
            int total = playbackController.currentManifest?.total_questions ?? 12;
            questionProgressText.text = $"Question {item.id} / {total}";
        }

        if (questionContentText != null)
        {
            questionContentText.text = $"\"{item.question}\"";
        }

        if (statusText != null)
        {
            string role = playbackController.currentManifest?.role;
            string roleSuffix = string.IsNullOrEmpty(role) ? string.Empty : $"  •  {role.ToUpperInvariant()}";
            string source = playbackController.currentManifest?.source;
            string mode = source == "openrouter" ? "AI-GENERATED" :
                source == "baked_fallback" ? "BAKED FALLBACK" : "BAKED";
            statusText.text = $"{mode}  •  {item.tone.ToUpperInvariant()}  •  {item.gesture.Replace('_', ' ')}{roleSuffix}";
        }

        if (nextButton != null)
        {
            Text buttonText = nextButton.GetComponentInChildren<Text>();
            bool isLast = playbackController.currentManifest != null &&
                playbackController.currentQuestionIndex >= playbackController.currentManifest.questions.Count - 1;
            if (buttonText != null) buttonText.text = isLast ? "Complete Interview" : "Next Question";
        }
    }

    private void HandlePlaybackFinished()
    {
        interviewPanel?.SetActive(false);
        completionPanel?.SetActive(true);
        if (completionSummaryText != null)
            completionSummaryText.text = $"You completed {playbackController.currentManifest?.total_questions ?? 0} questions in the {playbackController.currentManifest?.persona_name ?? "selected"} tone.";
    }

    private void HandleStatusChanged(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
