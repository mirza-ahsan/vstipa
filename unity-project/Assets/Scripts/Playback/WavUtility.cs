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

        // Parse RIFF header
        int channels = BitConverter.ToInt16(wavFileBytes, 22);
        int frequency = BitConverter.ToInt32(wavFileBytes, 24);
        int bitsPerSample = BitConverter.ToInt16(wavFileBytes, 34);

        // Find "data" subchunk
        int pos = 12;
        while (pos < wavFileBytes.Length - 8)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(wavFileBytes, pos, 4);
            int chunkSize = BitConverter.ToInt32(wavFileBytes, pos + 4);
            if (chunkId == "data")
            {
                pos += 8;
                int sampleCount = chunkSize / (bitsPerSample / 8);
                float[] samples = new float[sampleCount];

                if (bitsPerSample == 16)
                {
                    int sampleIndex = 0;
                    for (int i = pos; i < pos + chunkSize && i < wavFileBytes.Length - 1; i += 2)
                    {
                        short sample = BitConverter.ToInt16(wavFileBytes, i);
                        samples[sampleIndex++] = sample / 32768f;
                    }
                }
                else if (bitsPerSample == 8)
                {
                    int sampleIndex = 0;
                    for (int i = pos; i < pos + chunkSize && i < wavFileBytes.Length; i++)
                    {
                        samples[sampleIndex++] = (wavFileBytes[i] - 128) / 128f;
                    }
                }

                AudioClip audioClip = AudioClip.Create(clipName, samples.Length / channels, channels, frequency, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            }
            pos += 8 + chunkSize;
        }

        Debug.LogError("[WavUtility] 'data' chunk not found in WAV header.");
        return null;
    }
}
