using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PcmAudioPlayer : MonoBehaviour
{
    private readonly Queue<AudioClip> clipQueue = new Queue<AudioClip>();
    private AudioSource audioSource;
    private const int SampleRate = 16000;

    private void Awake() => audioSource = GetComponent<AudioSource>();

    private void Update()
    {
        if (!audioSource.isPlaying && clipQueue.Count > 0)
        {
            audioSource.clip = clipQueue.Dequeue();
            audioSource.Play();
        }
    }

    public void EnqueueBase64Audio(string base64Audio)
    {
        byte[] bytes   = System.Convert.FromBase64String(base64Audio);
        int    samples = bytes.Length / 2;
        float[] floats = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            short sample  = (short)((bytes[i * 2 + 1] << 8) | bytes[i * 2]);
            floats[i]     = sample / 32768f;
        }

        var clip = AudioClip.Create("ElevenLabsClip", samples, 1, SampleRate, false);
        clip.SetData(floats, 0);
        clipQueue.Enqueue(clip);
    }

    /** Clears queue and stops playback immediately */
    public void StopImmediately()
    {
        clipQueue.Clear();
        audioSource.Stop();
    }
}