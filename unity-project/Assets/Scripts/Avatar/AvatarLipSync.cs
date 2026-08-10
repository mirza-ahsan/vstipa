using UnityEngine;

public class AvatarLipSync : MonoBehaviour
{
    [Header("Target Head Skinned Mesh")]
    public SkinnedMeshRenderer headMeshRenderer;
    public AudioSource audioSource;

    [Header("Lip Sync Settings")]
    public float sensitivity = 40.0f;
    public float smoothing = 15.0f;
    public float maxBlendWeight = 100.0f;

    [Header("Blendshape Indices (-1 = auto-detect)")]
    public int visemeAaIndex = -1;
    public int visemeOIndex = -1;
    public int visemeEIndex = -1;
    public int visemeUIndex = -1;
    public int smileIndex = -1;

    [Header("Static-face fallback")]
    [Tooltip("A small mouth-opening mesh used only when the avatar has no facial morph targets.")]
    public Transform mouthProxy;
    public float proxyOpenScale = 3.4f;
    public float proxyWideScale = 1.35f;

    [Header("Diagnostics")]
    public bool hasRealVisemes;
    public bool isUsingULipSync;
    public string currentPhoneme = "-";

    private float currentAaWeight = 0f;
    private float currentOWeight = 0f;
    private float currentEWeight = 0f;
    private float currentUWeight = 0f;

    private float[] audioSamples = new float[256];
    private float[] spectrumSamples = new float[256];
    private Vector3 mouthProxyClosedScale;
    private float uLipSyncVolume;
    private float lastULipSyncUpdate = -10f;
    private bool loggedLiveAnalysis;

    private void Start()
    {
        if (headMeshRenderer == null || headMeshRenderer.sharedMesh == null || headMeshRenderer.sharedMesh.blendShapeCount == 0)
        {
            foreach (SkinnedMeshRenderer candidate in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (candidate.sharedMesh != null && candidate.sharedMesh.blendShapeCount > 0)
                {
                    headMeshRenderer = candidate;
                    break;
                }
            }
        }

        if (headMeshRenderer != null && headMeshRenderer.sharedMesh != null)
        {
            Mesh mesh = headMeshRenderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i).ToLower();
                if (shapeName.Contains("viseme_aa") || shapeName.Contains("jaw") || shapeName.Contains("open"))
                {
                    visemeAaIndex = i;
                }
                else if (shapeName.Contains("viseme_o") || shapeName.Contains("o_shape"))
                {
                    visemeOIndex = i;
                }
                else if (shapeName.Contains("viseme_e") || shapeName.Contains("e_shape"))
                {
                    visemeEIndex = i;
                }
                else if (shapeName.Contains("viseme_u") || shapeName.Contains("u_shape"))
                {
                    visemeUIndex = i;
                }
                else if (shapeName.Contains("smile"))
                {
                    smileIndex = i;
                }
            }
        }

        hasRealVisemes = headMeshRenderer != null &&
            (visemeAaIndex >= 0 || visemeOIndex >= 0 || visemeEIndex >= 0 || visemeUIndex >= 0);

        if (mouthProxy != null)
        {
            mouthProxyClosedScale = mouthProxy.localScale;
            mouthProxy.gameObject.SetActive(!hasRealVisemes);
        }

        Debug.Log(hasRealVisemes
            ? $"[AvatarLipSync] {name}: facial blendshape mode active."
            : $"[AvatarLipSync] {name}: no facial morph targets; using the visible mouth-proxy fallback.");
    }

    private void Update()
    {
        if (audioSource == null) return;

        float targetAa = 0f;
        float targetO = 0f;
        float targetE = 0f;
        float targetU = 0f;

        if (audioSource.isPlaying)
        {
            bool receivedRecentULipSync = Time.unscaledTime - lastULipSyncUpdate < 0.25f;
            isUsingULipSync = receivedRecentULipSync;

            if (receivedRecentULipSync)
            {
                targetAa = uLipSyncVolume * maxBlendWeight;
                switch (currentPhoneme.ToUpperInvariant())
                {
                    case "O": targetO = targetAa * 0.85f; break;
                    case "E":
                    case "I": targetE = targetAa * 0.75f; break;
                    case "U": targetU = targetAa * 0.75f; break;
                }
            }
            else
            {
                AnalyzeAudioFallback(out targetAa, out targetO, out targetE, out targetU);
            }
        }

        currentAaWeight = Mathf.Lerp(currentAaWeight, targetAa, Time.deltaTime * smoothing);
        currentOWeight = Mathf.Lerp(currentOWeight, targetO, Time.deltaTime * smoothing);
        currentEWeight = Mathf.Lerp(currentEWeight, targetE, Time.deltaTime * smoothing);
        currentUWeight = Mathf.Lerp(currentUWeight, targetU, Time.deltaTime * smoothing);

        if (hasRealVisemes && headMeshRenderer != null)
        {
            if (visemeAaIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeAaIndex, currentAaWeight);
            if (visemeOIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeOIndex, currentOWeight);
            if (visemeEIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeEIndex, currentEWeight);
            if (visemeUIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeUIndex, currentUWeight);
        }
        else
        {
            AnimateMouthProxy(currentAaWeight / Mathf.Max(maxBlendWeight, 0.001f));
        }
    }

    public void OnULipSyncUpdate(uLipSync.LipSyncInfo info)
    {
        currentPhoneme = string.IsNullOrEmpty(info.phoneme) ? "-" : info.phoneme;
        float logVolume = info.rawVolume > 0f ? Mathf.Log10(info.rawVolume) : -4f;
        uLipSyncVolume = Mathf.InverseLerp(-2.8f, -1.15f, logVolume);
        lastULipSyncUpdate = Time.unscaledTime;
        if (!loggedLiveAnalysis && info.rawVolume > 0.00001f)
        {
            loggedLiveAnalysis = true;
            Debug.Log($"[AvatarLipSync] {name}: uLipSync MFCC analysis is receiving live PCM samples (phoneme={currentPhoneme}).");
        }
    }

    private void AnalyzeAudioFallback(out float targetAa, out float targetO, out float targetE, out float targetU)
    {
        targetAa = targetO = targetE = targetU = 0f;

        bool sampled = false;
        try
        {
            audioSource.GetOutputData(audioSamples, 0);
            sampled = true;
        }
        catch (UnityException)
        {
            // WebGL does not always expose the output buffer; clip sampling below is deterministic.
        }

        float sum = 0f;
        if (sampled)
        {
            for (int i = 0; i < audioSamples.Length; i++) sum += audioSamples[i] * audioSamples[i];
        }

        if (sum < 0.000001f && audioSource.clip != null && audioSource.clip.samples > audioSamples.Length)
        {
            int offset = Mathf.Clamp(audioSource.timeSamples, 0, audioSource.clip.samples - audioSamples.Length - 1);
            if (audioSource.clip.GetData(audioSamples, offset))
            {
                sum = 0f;
                for (int i = 0; i < audioSamples.Length; i++) sum += audioSamples[i] * audioSamples[i];
            }
        }

        float rms = Mathf.Sqrt(sum / audioSamples.Length);

        try
        {
            audioSource.GetSpectrumData(spectrumSamples, 0, FFTWindow.BlackmanHarris);
        }
        catch (UnityException)
        {
            System.Array.Clear(spectrumSamples, 0, spectrumSamples.Length);
        }

        float lowFreqPower = 0f;
        float midFreqPower = 0f;
        float highFreqPower = 0f;
        for (int i = 1; i < 12; i++) lowFreqPower += spectrumSamples[i];
        for (int i = 12; i < 32; i++) midFreqPower += spectrumSamples[i];
        for (int i = 32; i < 64; i++) highFreqPower += spectrumSamples[i];

        targetAa = Mathf.Clamp(rms * sensitivity * 100.0f, 0f, maxBlendWeight);
        if (lowFreqPower > midFreqPower && lowFreqPower > highFreqPower)
            targetO = Mathf.Clamp(lowFreqPower * sensitivity * 200.0f, 0f, maxBlendWeight * 0.7f);
        else if (midFreqPower > highFreqPower)
            targetE = Mathf.Clamp(midFreqPower * sensitivity * 200.0f, 0f, maxBlendWeight * 0.7f);
        else
            targetU = Mathf.Clamp(highFreqPower * sensitivity * 200.0f, 0f, maxBlendWeight * 0.7f);
    }

    private void AnimateMouthProxy(float openAmount)
    {
        if (mouthProxy == null) return;

        float easedOpen = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(openAmount));
        float vowelWidth = currentPhoneme == "E" || currentPhoneme == "I" ? proxyWideScale : 1f;
        mouthProxy.localScale = new Vector3(
            mouthProxyClosedScale.x * vowelWidth,
            mouthProxyClosedScale.y * Mathf.Lerp(0.22f, proxyOpenScale, easedOpen),
            mouthProxyClosedScale.z);
    }

    public void SetPersonaBaselineSmile(float smileWeight)
    {
        if (headMeshRenderer != null && smileIndex >= 0)
        {
            headMeshRenderer.SetBlendShapeWeight(smileIndex, smileWeight);
        }
    }
}
