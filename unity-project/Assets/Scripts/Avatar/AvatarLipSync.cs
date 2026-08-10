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

    private float currentAaWeight = 0f;
    private float currentOWeight = 0f;
    private float currentEWeight = 0f;
    private float currentUWeight = 0f;

    private float[] audioSamples = new float[256];
    private float[] spectrumSamples = new float[256];

    private void Start()
    {
        if (headMeshRenderer == null)
        {
            headMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
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
    }

    private void Update()
    {
        if (headMeshRenderer == null || audioSource == null) return;

        float targetAa = 0f;
        float targetO = 0f;
        float targetE = 0f;
        float targetU = 0f;

        if (audioSource.isPlaying)
        {
            audioSource.GetOutputData(audioSamples, 0);

            // Compute RMS volume
            float sum = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                sum += audioSamples[i] * audioSamples[i];
            }
            float rms = Mathf.Sqrt(sum / audioSamples.Length);

            // Compute spectrum for vowel formant estimation
            audioSource.GetSpectrumData(spectrumSamples, 0, FFTWindow.BlackmanHarris);
            float lowFreqPower = 0f;  // ~100Hz - 800Hz (aa / open vowel)
            float midFreqPower = 0f;  // ~800Hz - 2500Hz (e / i vowel)
            float highFreqPower = 0f; // ~2500Hz - 5000Hz (u / rounded vowel)

            for (int i = 1; i < 12; i++) lowFreqPower += spectrumSamples[i];
            for (int i = 12; i < 32; i++) midFreqPower += spectrumSamples[i];
            for (int i = 32; i < 64; i++) highFreqPower += spectrumSamples[i];

            targetAa = Mathf.Clamp(rms * sensitivity * 100.0f, 0f, maxBlendWeight);

            if (lowFreqPower > midFreqPower && lowFreqPower > highFreqPower)
            {
                targetO = Mathf.Clamp(lowFreqPower * sensitivity * 200.0f, 0f, maxBlendWeight * 0.7f);
            }
            else if (midFreqPower > highFreqPower)
            {
                targetE = Mathf.Clamp(midFreqPower * sensitivity * 200.0f, 0f, maxBlendWeight * 0.7f);
            }
            else
            {
                targetU = Mathf.Clamp(highFreqPower * sensitivity * 200.0f, 0f, maxBlendWeight * 0.7f);
            }

            if (Time.frameCount % 45 == 0)
            {
                Debug.Log($"[AvatarLipSync] Real-time FFT LipSync Active: RMS={rms:F4}, viseme_aa={currentAaWeight:F1}, viseme_O={currentOWeight:F1}, viseme_E={currentEWeight:F1}, viseme_U={currentUWeight:F1}");
            }
        }

        currentAaWeight = Mathf.Lerp(currentAaWeight, targetAa, Time.deltaTime * smoothing);
        currentOWeight = Mathf.Lerp(currentOWeight, targetO, Time.deltaTime * smoothing);
        currentEWeight = Mathf.Lerp(currentEWeight, targetE, Time.deltaTime * smoothing);
        currentUWeight = Mathf.Lerp(currentUWeight, targetU, Time.deltaTime * smoothing);

        if (visemeAaIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeAaIndex, currentAaWeight);
        if (visemeOIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeOIndex, currentOWeight);
        if (visemeEIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeEIndex, currentEWeight);
        if (visemeUIndex >= 0) headMeshRenderer.SetBlendShapeWeight(visemeUIndex, currentUWeight);
    }

    public void SetPersonaBaselineSmile(float smileWeight)
    {
        if (headMeshRenderer != null && smileIndex >= 0)
        {
            headMeshRenderer.SetBlendShapeWeight(smileIndex, smileWeight);
        }
    }
}
