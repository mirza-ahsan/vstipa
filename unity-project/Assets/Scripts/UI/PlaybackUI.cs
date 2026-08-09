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

    [Header("Buttons")]
    public Button nextButton;

    private float lastAdvanceTime = 0f;
    private const float COOLDOWN_SEC = 0.5f;

    private void Start()
    {
        if (playbackController == null)
        {
            playbackController = FindFirstObjectByType<QuestionPlaybackController>();
        }

        if (playbackController != null)
        {
            playbackController.OnQuestionChanged += HandleQuestionChanged;
            playbackController.OnPlaybackFinished += HandlePlaybackFinished;
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
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

        if (vrInputDetected && (Time.time - lastAdvanceTime > COOLDOWN_SEC))
        {
            lastAdvanceTime = Time.time;
            OnNextButtonClicked();
        }
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
            statusText.text = $"Tone: {item.tone} | Gesture: {item.gesture}";
        }
    }

    private void HandlePlaybackFinished()
    {
        if (questionContentText != null)
        {
            questionContentText.text = "Interview Complete! Press any button to restart.";
        }

        if (statusText != null)
        {
            statusText.text = "Completed";
        }
    }
}
