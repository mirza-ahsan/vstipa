using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class QuestionPlaybackController : MonoBehaviour
{
    [Header("Configuration")]
    public string selectedPersona = "warm";
    public AudioSource audioSource;

    [Header("Avatar Components")]
    public AvatarGestureController activeAvatarGestureController;
    public AvatarLipSync activeAvatarLipSync;

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
        audioSource.spatialBlend = 0f; // 2D UI stereo playback

        BindActiveAvatar();
        LoadManifest(selectedPersona);
    }

    public void BindActiveAvatar()
    {
        if (activeAvatarGestureController == null)
        {
            activeAvatarGestureController = FindObjectOfType<AvatarGestureController>();
        }

        if (activeAvatarLipSync == null)
        {
            activeAvatarLipSync = FindObjectOfType<AvatarLipSync>();
        }

        if (activeAvatarLipSync != null)
        {
            activeAvatarLipSync.audioSource = audioSource;
        }
    }

    public void LoadManifest(string persona)
    {
        selectedPersona = persona;
        currentQuestionIndex = -1;

        string manifestUrl = GetStreamingAssetsUrl($"questions/{selectedPersona}/manifest.json");
        StartCoroutine(LoadManifestCoroutine(manifestUrl));
    }

    private string GetStreamingAssetsUrl(string relativePath)
    {
        string baseDir = Application.streamingAssetsPath;
        if (!baseDir.EndsWith("/"))
        {
            baseDir += "/";
        }
        return baseDir + relativePath;
    }

    private IEnumerator LoadManifestCoroutine(string url)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonText = www.downloadHandler.text;
                currentManifest = JsonUtility.FromJson<PersonaManifestData>(jsonText);
                Debug.Log($"[QuestionPlaybackController] Successfully loaded manifest from '{url}' with {currentManifest?.questions?.Count ?? 0} questions.");

                // Auto-start Question 1 immediately on app load
                AdvanceToNextQuestion();
            }
            else
            {
                Debug.LogError($"[QuestionPlaybackController] Failed to load manifest from '{url}': {www.error}");
            }
        }
    }

    public void AdvanceToNextQuestion()
    {
        if (currentManifest == null || currentManifest.questions == null || currentManifest.questions.Count == 0)
        {
            Debug.LogWarning("[QuestionPlaybackController] Manifest not ready.");
            return;
        }

        currentQuestionIndex++;
        if (currentQuestionIndex >= currentManifest.questions.Count)
        {
            Debug.Log("[QuestionPlaybackController] Reached end of questions set.");
            currentQuestionIndex = currentManifest.questions.Count - 1;
            OnPlaybackFinished?.Invoke();
            return;
        }

        QuestionItemData item = currentManifest.questions[currentQuestionIndex];
        OnQuestionChanged?.Invoke(item);

        // Trigger Avatar Gesture Animation
        if (activeAvatarGestureController != null && !string.IsNullOrEmpty(item.gesture))
        {
            activeAvatarGestureController.TriggerGesture(item.gesture);
            Debug.Log($"[QuestionPlaybackController] Triggered Avatar Gesture: '{item.gesture}' for Q{item.id:02d}");
        }

        StartCoroutine(PlayQuestionAudioCoroutine(item));
    }

    private IEnumerator PlayQuestionAudioCoroutine(QuestionItemData item)
    {
        isPlaying = true;
        audioSource.Stop();

        string audioUrl = GetStreamingAssetsUrl($"questions/{selectedPersona}/{item.audio_file}");
        Debug.Log($"[QuestionPlaybackController] Loading spoken voice audio clip from URL: {audioUrl}");

        AudioType audioType = item.audio_file.EndsWith(".mp3") ? AudioType.MPEG : AudioType.WAV;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioUrl, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null && clip.length > 0)
                {
                    audioSource.clip = clip;
                    audioSource.volume = 1.0f;
                    audioSource.Play();
                    Debug.Log($"[QuestionPlaybackController] PLAYING SPOKEN VOICE Q{item.id:02d} SUCCESS: '{item.question}' ({clip.length:F2}s, {clip.frequency} Hz)");
                }
                else
                {
                    Debug.LogError($"[QuestionPlaybackController] GetContent returned empty clip for {audioUrl}");
                }
            }
            else
            {
                Debug.LogError($"[QuestionPlaybackController] Failed to fetch audio WebRequest from {audioUrl}: {www.error}");
            }
        }

        isPlaying = false;
    }
}
