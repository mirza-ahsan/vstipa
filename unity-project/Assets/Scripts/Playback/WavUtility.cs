using System;
using UnityEngine;

public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavFileBytes, string clipName = "wavClip")
    {
        if (wavFileBytes == null || wavFileBytes.Length < 44)
        {
            Debug.LogError("[WavUtility] Invalid WAV byte array.");
            return null;
        }

        string riff = System.Text.Encoding.ASCII.GetString(wavFileBytes, 0, 4);
        string wave = System.Text.Encoding.ASCII.GetString(wavFileBytes, 8, 4);

        if (riff != "RIFF" || wave != "WAVE")
        {
            Debug.LogError($"[WavUtility] Not a valid WAV file: riff={riff}, wave={wave}");
            return null;
        }

        int channels = BitConverter.ToInt16(wavFileBytes, 22);
        int frequency = BitConverter.ToInt32(wavFileBytes, 24);
        int bitsPerSample = BitConverter.ToInt16(wavFileBytes, 34);

        int pos = 12;
        while (pos <= wavFileBytes.Length - 8)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(wavFileBytes, pos, 4);
            int chunkSize = BitConverter.ToInt32(wavFileBytes, pos + 4);
            pos += 8;

            if (chunkId == "data")
            {
                int totalSamples = chunkSize / (bitsPerSample / 8);
                float[] samples = new float[totalSamples];

                if (bitsPerSample == 16)
                {
                    int sampleIndex = 0;
                    for (int i = pos; i < pos + chunkSize && i < wavFileBytes.Length - 1 && sampleIndex < totalSamples; i += 2)
                    {
                        short sample16 = BitConverter.ToInt16(wavFileBytes, i);
                        samples[sampleIndex++] = sample16 / 32768.0f;
                    }
                }

                AudioClip audioClip = AudioClip.Create(clipName, totalSamples / channels, channels, frequency, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            }

            pos += (chunkSize + 1) & ~1;
        }

        Debug.LogError("[WavUtility] 'data' chunk not found in WAV file.");
        return null;
    }
}
