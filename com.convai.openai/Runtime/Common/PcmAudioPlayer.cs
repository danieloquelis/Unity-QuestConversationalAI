using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Handles playback of PCM audio received as Base64-encoded strings.
/// Uses an internal queue to play audio clips sequentially through an AudioSource.
/// </summary>
public class PcmAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private int sampleRate = 16000;
    
    private readonly Queue<AudioClip> _clipQueue = new();
    
    #region Unity Lifecycle

    private void Start()
    {
        Assert.IsNotNull(audioSource, "Audio source is required");
    }

    private void Update()
    {
        if (audioSource.isPlaying || _clipQueue.Count == 0)
            return;

        audioSource.clip = _clipQueue.Dequeue();
        audioSource.Play();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Converts Base64-encoded PCM audio into a Unity AudioClip 
    /// and adds it to the playback queue.
    /// </summary>
    /// <param name="base64Audio">Base64-encoded PCM16 audio data.</param>
    public void EnqueueBase64Audio(string base64Audio)
    {
        // Decode Base64 string into raw bytes
        var bytes = System.Convert.FromBase64String(base64Audio);

        // Each PCM16 sample uses 2 bytes
        var sampleCount = bytes.Length / 2;
        var samples = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = (short)((bytes[i * 2 + 1] << 8) | bytes[i * 2]);
            samples[i] = sample / 32768f;
        }

        var clip = AudioClip.Create("AIConversationalClip", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        _clipQueue.Enqueue(clip);
    }

    /// <summary>
    /// Stops playback immediately and clears any queued audio.
    /// </summary>
    public void StopImmediately()
    {
        _clipQueue.Clear();
        audioSource.Stop();
    }

    #endregion
}
