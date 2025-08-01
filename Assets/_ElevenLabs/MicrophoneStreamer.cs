using System;
using UnityEngine;

/// <summary>
/// Streams microphone audio to a callback as 16-bit mono PCM
/// Base64-encoded, exactly 1 024 samples (≈64 ms at 16 kHz) per chunk.
/// </summary>
public class MicrophoneStreamer : MonoBehaviour
{
    /* --------------------------------------------------------------------- */
    /*                               Public API                              */
    /* --------------------------------------------------------------------- */

    /// <summary>Invoked every time a 1 024-sample 16 kHz chunk is ready.</summary>
    public Action<string> OnAudioChunk;

    [Header("Silence detection (dBFS, ms)")]
    public float SilenceThresholdDb = -32f;   // louder than typical speaker bleed
    public int   SilenceDurationMs  = 500;

    /* --------------------------------------------------------------------- */
    /*                          Implementation details                       */
    /* --------------------------------------------------------------------- */

    private const int SampleRateOut     = 16_000; // ElevenLabs requirement
    private const int ChunkSamplesOut   = 1_024;  // 64 ms @ 16 kHz

    private AudioClip microphoneClip;
    private string    micDevice;

    private int micSampleRate;   // actual hardware rate (e.g. 44 100 / 48 000)
    private int chunkSamplesIn;  // how many mic-rate samples ≈ 1 024 @16 kHz
    private int lastSamplePos;

    private SilenceDetector silenceDetector;

    /* ---------------------------- Unity lifecycle ------------------------- */

    private void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("MicrophoneStreamer: no microphone devices.");
            enabled = false;
            return;
        }

        micDevice        = Microphone.devices[0];
        silenceDetector  = new SilenceDetector(SilenceThresholdDb, SilenceDurationMs);
    }

    public void StartStreaming()
    {
        // Unity may ignore the requested rate and pick the closest supported!
        microphoneClip  = Microphone.Start(micDevice, true, 1, SampleRateOut);
        micSampleRate   = microphoneClip.frequency;
        chunkSamplesIn  = Mathf.RoundToInt(ChunkSamplesOut *
                            (float)micSampleRate / SampleRateOut);

        lastSamplePos   = 0;
        silenceDetector.Reset();

        Debug.Log($"[MicrophoneStreamer] device={micDevice}  realRate={micSampleRate} Hz  " +
                  $"chunkIn={chunkSamplesIn} samples");
    }

    public void StopStreaming()
    {
        if (Microphone.IsRecording(micDevice))
            Microphone.End(micDevice);

        microphoneClip = null;
        silenceDetector.Reset();
    }

    private void Update()
    {
        if (microphoneClip == null) return;

        int currentPos       = Microphone.GetPosition(micDevice);
        int samplesAvailable = currentPos - lastSamplePos;
        if (samplesAvailable < 0) samplesAvailable += microphoneClip.samples;

        if (samplesAvailable >= chunkSamplesIn)
        {
            /* -------- read a circular chunk from the Unity mic buffer ------ */
            float[] inBuf = new float[chunkSamplesIn];
            ReadCircular(microphoneClip, lastSamplePos, inBuf);
            lastSamplePos = (lastSamplePos + chunkSamplesIn) % microphoneClip.samples;

            /* --------------------- silence gating -------------------------- */
            if (silenceDetector.IsSilent(inBuf)) return;

            /* ------------- down-sample to 16 kHz + convert to PCM ---------- */
            byte[] pcm16 = DownsampleAndConvert(inBuf, micSampleRate, SampleRateOut);
            OnAudioChunk?.Invoke(Convert.ToBase64String(pcm16));
        }
    }

    /* --------------------------------------------------------------------- */
    /*                             Helper methods                            */
    /* --------------------------------------------------------------------- */

    /// <summary>Reads <paramref name="buffer"/> from <paramref name="clip"/> starting
    /// at sample index <paramref name="start"/>, wrapping around the circular buffer.</summary>
    private static void ReadCircular(AudioClip clip, int start, float[] buffer)
    {
        int len         = buffer.Length;
        int clipSamples = clip.samples;
        int tail        = clipSamples - start;

        if (len <= tail)
        {
            clip.GetData(buffer, start);
        }
        else
        {
            float[] tempTail = new float[tail];
            float[] tempHead = new float[len - tail];
            clip.GetData(tempTail, start);
            clip.GetData(tempHead, 0);
            Array.Copy(tempTail, 0, buffer, 0, tail);
            Array.Copy(tempHead, 0, buffer, tail, tempHead.Length);
        }
    }

    /// <summary>
    /// Converts <paramref name="inBuf"/> from <paramref name="inRate"/> to
    /// <paramref name="outRate"/> (mono) and packs into little-endian 16-bit PCM.
    /// Linear interpolation is used to handle non-integer ratios.
    /// </summary>
    private static byte[] DownsampleAndConvert(float[] inBuf, int inRate, int outRate)
    {
        if (inRate == outRate) return ConvertToPcm16(inBuf);

        float ratio   = (float)inRate / outRate;
        int   outLen  = Mathf.RoundToInt(inBuf.Length / ratio);
        byte[] pcmOut = new byte[outLen * 2];

        float pos = 0f;
        for (int o = 0; o < outLen; o++, pos += ratio)
        {
            int i0    = Mathf.Clamp((int)pos, 0, inBuf.Length - 1);
            int i1    = Mathf.Min(i0 + 1, inBuf.Length - 1);
            float frac = pos - i0;

            // Linear interpolation between neighbouring samples
            float sample = Mathf.Lerp(inBuf[i0], inBuf[i1], frac);
            short s16    = (short)Mathf.Clamp(sample * 32767f,
                               short.MinValue, short.MaxValue);

            pcmOut[o * 2]     = (byte)(s16 & 0xFF);
            pcmOut[o * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }
        return pcmOut;
    }

    private static byte[] ConvertToPcm16(float[] buf)
    {
        byte[] pcm = new byte[buf.Length * 2];
        for (int i = 0; i < buf.Length; i++)
        {
            short s = (short)Mathf.Clamp(buf[i] * 32767f,
                          short.MinValue, short.MaxValue);
            pcm[i * 2]     = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return pcm;
    }

    /* --------------------------------------------------------------------- */
    /*                         Nested silence detector                       */
    /* --------------------------------------------------------------------- */

    private class SilenceDetector
    {
        private readonly float thresholdDb;
        private readonly int   durationMs;
        private double?        startMs;

        public SilenceDetector(float thresholdDb, int durationMs)
        {
            this.thresholdDb = thresholdDb;
            this.durationMs  = durationMs;
        }

        public bool IsSilent(float[] samples)
        {
            double sum = 0;
            for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];

            double rms = Math.Sqrt(sum / samples.Length);
            double db  = 20 * Math.Log10(rms + 1e-12);

            double nowMs = Time.realtimeSinceStartupAsDouble * 1000.0;

            if (db < thresholdDb)
            {
                if (startMs == null) startMs = nowMs;
                else if (nowMs - startMs >= durationMs) return true;
            }
            else
            {
                startMs = null;
            }
            return false;
        }

        public void Reset() => startMs = null;
    }
}
