using System;
using UnityEngine;

namespace ElevenLabs
{
    /// <summary>
    /// Streams microphone audio as Base64-encoded 16-bit mono PCM chunks.
    /// Each chunk is 1,024 samples (~64 ms at 16 kHz).
    /// </summary>
    public class MicrophoneStreamer : MonoBehaviour
    {
        public Action<string> OnAudioChunk;
    
        private const int SampleRateOut   = 16_000; 
        private const int ChunkSamplesOut = 1_024;

        private AudioClip _microphoneClip;
        private string _micDevice;
        private int _micSampleRate;   
        private int _chunkSamplesIn;  
        private int _lastSamplePos;

        #region Unity Lifecycle

        private void Start()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("MicrophoneStreamer: no microphone devices.");
                enabled = false;
                return;
            }

            _micDevice = Microphone.devices[0];
        }

        private void Update()
        {
            if (!_microphoneClip) return;

            var currentPos       = Microphone.GetPosition(_micDevice);
            var samplesAvailable = currentPos - _lastSamplePos;
            if (samplesAvailable < 0) 
                samplesAvailable += _microphoneClip.samples;

            if (samplesAvailable < _chunkSamplesIn) return;
        
            var inBuf = new float[_chunkSamplesIn];
            ReadCircular(_microphoneClip, _lastSamplePos, inBuf);
            _lastSamplePos = (_lastSamplePos + _chunkSamplesIn) % _microphoneClip.samples;
            var pcm16 = DownsampleAndConvert(inBuf, _micSampleRate, SampleRateOut);
            OnAudioChunk?.Invoke(Convert.ToBase64String(pcm16));
        }

        #endregion

        #region Public Methods

        public void StartStreaming()
        {
            _microphoneClip = Microphone.Start(_micDevice, true, 1, SampleRateOut);
            _micSampleRate  = _microphoneClip.frequency;
            _chunkSamplesIn = Mathf.RoundToInt(ChunkSamplesOut * (float)_micSampleRate / SampleRateOut);
            _lastSamplePos  = 0;

            Debug.Log($"[MicrophoneStreamer] device={_micDevice}, " +
                      $"realRate={_micSampleRate} Hz, chunkIn={_chunkSamplesIn} samples");
        }

        public void StopStreaming()
        {
            if (Microphone.IsRecording(_micDevice))
                Microphone.End(_micDevice);

            _microphoneClip = null;
        }

        #endregion

        #region Helpers

        private static void ReadCircular(AudioClip clip, int start, float[] buffer)
        {
            var len         = buffer.Length;
            var clipSamples = clip.samples;
            var tail        = clipSamples - start;

            if (len <= tail)
            {
                clip.GetData(buffer, start);
            }
            else
            {
                var tempTail = new float[tail];
                var tempHead = new float[len - tail];

                clip.GetData(tempTail, start);
                clip.GetData(tempHead, 0);

                Array.Copy(tempTail, 0, buffer, 0, tail);
                Array.Copy(tempHead, 0, buffer, tail, tempHead.Length);
            }
        }

        private static byte[] DownsampleAndConvert(float[] inBuf, int inRate, int outRate)
        {
            if (inRate == outRate) 
                return ConvertToPcm16(inBuf);

            var ratio   = (float)inRate / outRate;
            var outLen    = Mathf.RoundToInt(inBuf.Length / ratio);
            var pcmOut = new byte[outLen * 2];

            var pos = 0f;
            for (var o = 0; o < outLen; o++, pos += ratio)
            {
                var i0 = Mathf.Clamp((int)pos, 0, inBuf.Length - 1);
                var i1 = Mathf.Min(i0 + 1, inBuf.Length - 1);
                var frac = pos - i0;

                var sample = Mathf.Lerp(inBuf[i0], inBuf[i1], frac);
                var s16 = (short)Mathf.Clamp(sample * 32767f, short.MinValue, short.MaxValue);

                pcmOut[o * 2]     = (byte)(s16 & 0xFF);
                pcmOut[o * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
            }

            return pcmOut;
        }

        private static byte[] ConvertToPcm16(float[] buf)
        {
            var pcm = new byte[buf.Length * 2];
            for (var i = 0; i < buf.Length; i++)
            {
                var s = (short)Mathf.Clamp(buf[i] * 32767f, short.MinValue, short.MaxValue);
                pcm[i * 2]     = (byte)(s & 0xFF);
                pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }
            return pcm;
        }

        #endregion
    }
}
