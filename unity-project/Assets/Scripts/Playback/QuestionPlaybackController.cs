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
    public bool isInitialized = false;

    public event Action<QuestionItemData> OnQuestionChanged;
    public event Action OnPlaybackFinished;

    private string localQuestionsDir;

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

        localQuestionsDir = Path.Combine(Application.persistentDataPath, "questions");

        StartCoroutine(InitializeAndLoadCoroutine());
    }

    private IEnumerator InitializeAndLoadCoroutine()
    {
        // Copy StreamingAssets to persistentDataPath on Android/Quest so native FMOD audio engine can load WAV files directly
        yield return StartCoroutine(CopyPersonaAssetsIfNeeded(selectedPersona));

        isInitialized = true;
        LoadManifest(selectedPersona);
    }

    private IEnumerator CopyPersonaAssetsIfNeeded(string persona)
    {
        string targetPersonaDir = Path.Combine(localQuestionsDir, persona);
        Directory.CreateDirectory(targetPersonaDir);

        string srcManifestPath = Path.Combine(Application.streamingAssetsPath, "questions", persona, "manifest.json");
        string dstManifestPath = Path.Combine(targetPersonaDir, "manifest.json");

        // Copy manifest
        using (UnityWebRequest www = UnityWebRequest.Get(srcManifestPath))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(dstManifestPath, www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[QuestionPlaybackController] Error copying manifest: {www.error}");
            }
        }

        // Copy q01.wav .. q12.wav
        for (int i = 1; i <= 12; i++)
        {
            string fileName = $"q{i:02d}.wav";
            string srcAudioPath = Path.Combine(Application.streamingAssetsPath, "questions", persona, fileName);
            string dstAudioPath = Path.Combine(targetPersonaDir, fileName);

            if (!File.Exists(dstAudioPath))
            {
                using (UnityWebRequest www = UnityWebRequest.Get(srcAudioPath))
                {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(dstAudioPath, www.downloadHandler.data);
                    }
                }
            }
        }

        Debug.Log($"[QuestionPlaybackController] Extracted offline assets for persona '{persona}' to persistent storage.");
    }

    public void LoadManifest(string persona)
    {
        selectedPersona = persona;
        currentQuestionIndex = -1;
        string manifestPath = Path.Combine(localQuestionsDir, selectedPersona, "manifest.json");

        if (File.Exists(manifestPath))
        {
            string jsonText = File.ReadAllText(manifestPath);
            currentManifest = JsonUtility.FromJson<PersonaManifestData>(jsonText);
            Debug.Log($"[QuestionPlaybackController] Loaded manifest for persona '{selectedPersona}' ({currentManifest?.questions?.Count ?? 0} questions).");
        }
        else
        {
            Debug.LogError($"[QuestionPlaybackController] Local manifest not found at: {manifestPath}");
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
            Debug.Log("[QuestionPlaybackController] Reached end of questions.");
            currentQuestionIndex = currentManifest.questions.Count - 1;
            OnPlaybackFinished?.Invoke();
            return;
        }

        QuestionItemData item = currentManifest.questions[currentQuestionIndex];
        OnQuestionChanged?.Invoke(item);
        StartCoroutine(PlayQuestionAudioNativeCoroutine(item));
    }

    private IEnumerator PlayQuestionAudioNativeCoroutine(QuestionItemData item)
    {
        isPlaying = true;
        audioSource.Stop();

        string audioPath = Path.Combine(localQuestionsDir, selectedPersona, item.audio_file);
        string uriPath = "file://" + audioPath;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null && clip.length > 0)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log($"[QuestionPlaybackController] Playing Q{item.id:02d} clear audio: '{item.question}' ({clip.length:F2}s, {clip.frequency} Hz)");
                }
                else
                {
                    Debug.LogError($"[QuestionPlaybackController] DownloadHandlerAudioClip returned null clip for {uriPath}");
                }
            }
            else
            {
                Debug.LogError($"[QuestionPlaybackController] Failed to load audio clip via FMOD from {uriPath}: {www.error}");
            }
        }

        isPlaying = false;
    }
}
