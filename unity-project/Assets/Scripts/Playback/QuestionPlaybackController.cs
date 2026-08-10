using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class QuestionPlaybackController : MonoBehaviour
{
    [Header("Configuration")]
    public string selectedPersona = "warm";
    public AudioSource audioSource;
    public float audioGainMultiplier = 3.5f;
    public bool loadOnStart = true;
    public bool autoStartOnManifestLoad = true;

    [Header("Avatar Components")]
    public AvatarGestureController activeAvatarGestureController;
    public AvatarLipSync activeAvatarLipSync;

    [Header("State")]
    public PersonaManifestData currentManifest;
    public int currentQuestionIndex = -1;
    public bool isPlaying = false;

    public event Action<QuestionItemData> OnQuestionChanged;
    public event Action OnPlaybackFinished;
    public event Action<PersonaManifestData> OnManifestLoaded;

    private Coroutine manifestLoadCoroutine;
    private Coroutine audioLoadCoroutine;

    private void Start()
    {
        AudioListener.volume = 1.0f;
        AudioListener.pause = false;

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
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;

        BindActiveAvatar();
        if (loadOnStart) LoadManifest(selectedPersona);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[QuestionPlaybackController] OnApplicationFocus: {hasFocus}");
        if (hasFocus)
        {
            AudioListener.pause = false;
            AudioListener.volume = 1.0f;
            if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    public void BindActiveAvatar()
    {
        if (activeAvatarGestureController == null)
        {
            activeAvatarGestureController = FindAnyObjectByType<AvatarGestureController>();
        }

        if (activeAvatarLipSync == null)
        {
            activeAvatarLipSync = FindAnyObjectByType<AvatarLipSync>();
        }

        if (activeAvatarLipSync != null)
        {
            activeAvatarLipSync.audioSource = audioSource;
        }
    }

    public void LoadManifest(string persona)
    {
        StopPlayback();
        selectedPersona = persona;
        currentQuestionIndex = -1;
        currentManifest = null;

        string manifestUrl = GetStreamingAssetsUrl($"questions/{selectedPersona}/manifest.json");
        if (manifestLoadCoroutine != null) StopCoroutine(manifestLoadCoroutine);
        manifestLoadCoroutine = StartCoroutine(LoadManifestCoroutine(manifestUrl));
    }

    private string GetStreamingAssetsUrl(string relativePath)
    {
        string baseDir = Application.streamingAssetsPath;
        if (!baseDir.EndsWith("/"))
        {
            baseDir += "/";
        }
        string combined = baseDir + relativePath;
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!combined.Contains("://")) combined = "file://" + combined;
#endif
        return combined;
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
                OnManifestLoaded?.Invoke(currentManifest);

                if (autoStartOnManifestLoad) AdvanceToNextQuestion();
            }
            else
            {
                Debug.LogError($"[QuestionPlaybackController] Failed to load manifest from '{url}': {www.error}");
            }
        }
    }

    public void RestartPlayback()
    {
        currentQuestionIndex = -1;
        AdvanceToNextQuestion();
    }

    public void StopPlayback()
    {
        if (audioLoadCoroutine != null)
        {
            StopCoroutine(audioLoadCoroutine);
            audioLoadCoroutine = null;
        }
        if (audioSource != null) audioSource.Stop();
        isPlaying = false;
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
            currentQuestionIndex = currentManifest.questions.Count - 1;
            Debug.Log("[QuestionPlaybackController] Reached end of question set.");
            OnPlaybackFinished?.Invoke();
            return;
        }

        QuestionItemData item = currentManifest.questions[currentQuestionIndex];
        OnQuestionChanged?.Invoke(item);

        // Trigger Avatar Gesture Animation
        if (activeAvatarGestureController != null && !string.IsNullOrEmpty(item.gesture))
        {
            activeAvatarGestureController.TriggerGesture(item.gesture);
            Debug.Log($"[QuestionPlaybackController] Triggered Avatar Gesture: '{item.gesture}' for Q{item.id:D2}");
        }

        if (audioSource != null)
        {
            if (audioLoadCoroutine != null) StopCoroutine(audioLoadCoroutine);
            audioLoadCoroutine = StartCoroutine(PlayQuestionAudioCoroutine(item));
        }
    }

    private IEnumerator PlayQuestionAudioCoroutine(QuestionItemData item)
    {
        isPlaying = true;
        audioSource.Stop();

        string audioUrl = GetStreamingAssetsUrl($"questions/{selectedPersona}/{item.audio_file}");
        Debug.Log($"[QuestionPlaybackController] Loading spoken voice audio clip from URL: {audioUrl}");

        bool isWav = item.audio_file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);
        AudioType audioType = item.audio_file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? AudioType.MPEG : AudioType.WAV;

        using (UnityWebRequest www = isWav
            ? UnityWebRequest.Get(audioUrl)
            : UnityWebRequestMultimedia.GetAudioClip(audioUrl, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Unity WebGL can expose downloaded MP3/WAV clips as zero-length WebAudio
                // proxies. Parsing baked PCM WAV bytes ourselves gives uLipSync readable,
                // deterministic samples on every target.
                AudioClip clip = isWav
                    ? WavUtility.ToAudioClip(www.downloadHandler.data, $"{selectedPersona}_q{item.id:D2}")
                    : DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    if (clip.length <= 0f)
                    {
                        Debug.LogWarning($"[QuestionPlaybackController] Downloaded {audioUrl}, but this platform currently reports a zero duration. Attempting playback anyway.");
                    }

                    AmplifyAudioClip(clip, audioGainMultiplier);
                    AudioListener.pause = false;
                    AudioListener.volume = 1.0f;
                    audioSource.clip = clip;
                    audioSource.volume = 1.0f;
                    audioSource.Play();
                    Debug.Log($"[QuestionPlaybackController] PLAYING SPOKEN VOICE Q{item.id:D2} SUCCESS (Gain={audioGainMultiplier:F1}x): '{item.question}' ({clip.length:F2}s, {clip.frequency} Hz)");
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
        audioLoadCoroutine = null;
    }

    private void AmplifyAudioClip(AudioClip clip, float gain)
    {
        if (clip == null || gain <= 1.0f) return;

        try
        {
            float[] samples = new float[clip.samples * clip.channels];
            if (clip.GetData(samples, 0))
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = Mathf.Clamp(samples[i] * gain, -1.0f, 1.0f);
                }
                clip.SetData(samples, 0);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[QuestionPlaybackController] Could not amplify audio clip: {ex.Message}");
        }
    }
}
