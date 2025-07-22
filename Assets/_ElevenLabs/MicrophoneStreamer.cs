using System;
using UnityEngine;

public class MicrophoneStreamer : MonoBehaviour
{
    public Action<string> OnAudioChunk;

    private const int SampleRate = 16000;
    private const int ChunkSize = 1024;
    private AudioClip microphoneClip;
    private string micDevice;
    private int lastSamplePosition;

    private const float DefaultSilenceThresholdDb = -50f; // dB, tweak as needed
    private const int DefaultSilenceDurationMs = 300; // ms, tweak as needed

    public float SilenceThresholdDb = DefaultSilenceThresholdDb;
    public int SilenceDurationMs = DefaultSilenceDurationMs;

    private SilenceDetector silenceDetector;

    void Start()
    {
        if (Microphone.devices.Length > 0)
            micDevice = Microphone.devices[0];
        silenceDetector = new SilenceDetector(SilenceThresholdDb, SilenceDurationMs);
    }

    public void StartStreaming()
    {
        microphoneClip = Microphone.Start(micDevice, true, 1, SampleRate);
        lastSamplePosition = 0;
        silenceDetector?.Reset();
    }

    public void StopStreaming()
    {
        if (Microphone.IsRecording(micDevice))
            Microphone.End(micDevice);
        silenceDetector?.Reset();
    }

    void Update()
    {
        if (microphoneClip == null) return;

        int currentPos = Microphone.GetPosition(micDevice);
        int samplesAvailable = currentPos - lastSamplePosition;
        if (samplesAvailable < 0)
            samplesAvailable += microphoneClip.samples;

        if (samplesAvailable >= ChunkSize)
        {
            float[] samples = new float[ChunkSize];
            microphoneClip.GetData(samples, lastSamplePosition);

            // --- Silence detection using RMS in dB ---
            if (!silenceDetector.IsSilent(samples))
            {
                var int16Buffer = DownsampleAndConvert(samples, SampleRate, SampleRate);
                string base64 = Convert.ToBase64String(int16Buffer);
                OnAudioChunk?.Invoke(base64);
            }
            // --- End silence detection ---

            lastSamplePosition = (lastSamplePosition + ChunkSize) % microphoneClip.samples;
        }
    }

    private byte[] DownsampleAndConvert(float[] buffer, int inRate, int outRate)
    {
        if (inRate != outRate) throw new NotImplementedException("Downsampling not yet implemented");

        byte[] pcmBytes = new byte[buffer.Length * 2];
        for (int i = 0; i < buffer.Length; i++)
        {
            short val = (short)Mathf.Clamp(buffer[i] * 32767, short.MinValue, short.MaxValue);
            pcmBytes[i * 2] = (byte)(val & 0xff);
            pcmBytes[i * 2 + 1] = (byte)((val >> 8) & 0xff);
        }
        return pcmBytes;
    }

    // --- SilenceDetector class ---
    private class SilenceDetector
    {
        private readonly float thresholdDb;
        private readonly int durationMs;
        private double? silenceStartTime;

        public SilenceDetector(float thresholdDb, int durationMs)
        {
            this.thresholdDb = thresholdDb;
            this.durationMs = durationMs;
            this.silenceStartTime = null;
        }

        public bool IsSilent(float[] samples)
        {
            // Calculate RMS
            double sum = 0;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];
            double rms = Math.Sqrt(sum / samples.Length);
            double db = 20 * Math.Log10(rms + 1e-10); // avoid log(0)

            double now = Time.realtimeSinceStartupAsDouble * 1000.0;

            if (db < thresholdDb)
            {
                if (silenceStartTime == null)
                {
                    silenceStartTime = now;
                }
                else if (now - silenceStartTime >= durationMs)
                {
                    return true; // silent for long enough
                }
            }
            else
            {
                silenceStartTime = null;
            }
            return false;
        }

        public void Reset()
        {
            silenceStartTime = null;
        }
    }
}
