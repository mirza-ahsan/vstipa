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

    private float lastAdvanceTime = 0f;
    private const float COOLDOWN_SEC = 0.5f;

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
        bool vrInputDetected = Input.GetButtonDown("Fire1") ||
                               Input.GetButtonDown("Submit") ||
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
            playbackController?.currentManifest != null && (Time.time - lastAdvanceTime > COOLDOWN_SEC))
        {
            lastAdvanceTime = Time.time;
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
        if (startStatusText != null) startStatusText.text = "Enter a target role, then choose an interviewer style.";
    }

    public void RestartInterview()
    {
        completionPanel?.SetActive(false);
        interviewPanel?.SetActive(true);
        playbackController?.RestartPlayback();
    }

    public void OnNextButtonClicked()
    {
        if (playbackController != null)
        {
            playbackController.AdvanceToNextQuestion();
        }
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
            completionSummaryText.text = $"You completed {playbackController.currentManifest?.total_questions ?? 0} questions with the {playbackController.currentManifest?.persona_name ?? "selected"} persona.";
    }

    private void HandleStatusChanged(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
