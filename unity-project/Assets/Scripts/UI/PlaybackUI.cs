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
        // Support keyboard space / return or Quest trigger button for advancing questions in VR
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
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
            questionContentText.text = "Interview Complete! Press button to restart.";
        }

        if (statusText != null)
        {
            statusText.text = "Completed";
        }
    }
}
