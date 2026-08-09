using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class QuestionPlaybackController : MonoBehaviour
{
    [Header("Configuration")]
    public string selectedPersona = "warm";
    public AudioSource audioSource;

    [Header("State")]
    public PersonaManifestData currentManifest;
    public int currentQuestionIndex = -1;
    public bool isPlaying = false;

    public event Action<QuestionItemData> OnQuestionChanged;
    public event Action OnPlaybackFinished;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1.0f;

        LoadManifest(selectedPersona);
    }

    public void LoadManifest(string persona)
    {
        selectedPersona = persona;
        currentQuestionIndex = -1;
        string manifestPath = Path.Combine(Application.streamingAssetsPath, "questions", selectedPersona, "manifest.json");

        StartCoroutine(LoadManifestCoroutine(manifestPath));
    }

    private IEnumerator LoadManifestCoroutine(string path)
    {
        string jsonText = "";

        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                jsonText = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"[QuestionPlaybackController] Error loading manifest from {path}: {www.error}");
                yield break;
            }
        }

        currentManifest = JsonUtility.FromJson<PersonaManifestData>(jsonText);
        Debug.Log($"[QuestionPlaybackController] Loaded manifest for persona '{selectedPersona}' with {currentManifest?.questions?.Count ?? 0} questions.");
    }

    public void AdvanceToNextQuestion()
    {
        if (currentManifest == null || currentManifest.questions == null || currentManifest.questions.Count == 0)
        {
            Debug.LogWarning("[QuestionPlaybackController] No manifest loaded.");
            return;
        }

        currentQuestionIndex++;
        if (currentQuestionIndex >= currentManifest.questions.Count)
        {
            Debug.Log("[QuestionPlaybackController] Reached end of question set.");
            currentQuestionIndex = currentManifest.questions.Count - 1;
            OnPlaybackFinished?.Invoke();
            return;
        }

        QuestionItemData item = currentManifest.questions[currentQuestionIndex];
        OnQuestionChanged?.Invoke(item);
        StartCoroutine(PlayQuestionAudioCoroutine(item));
    }

    private IEnumerator PlayQuestionAudioCoroutine(QuestionItemData item)
    {
        isPlaying = true;
        audioSource.Stop();

        string audioPath = Path.Combine(Application.streamingAssetsPath, "questions", selectedPersona, item.audio_file);

        using (UnityWebRequest www = UnityWebRequest.Get(audioPath))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                byte[] bytes = www.downloadHandler.data;
                AudioClip clip = WavUtility.ToAudioClip(bytes, $"Q{item.id:02d}");

                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log($"[QuestionPlaybackController] Playing Q{item.id:02d}: '{item.question}' ({clip.samples} samples, {clip.frequency} Hz)");
                }
                else
                {
                    Debug.LogError($"[QuestionPlaybackController] Failed to parse WAV clip Q{item.id:02d}");
                }
            }
            else
            {
                Debug.LogError($"[QuestionPlaybackController] Failed to load audio clip from {audioPath}: {www.error}");
            }
        }

        isPlaying = false;
    }
}
