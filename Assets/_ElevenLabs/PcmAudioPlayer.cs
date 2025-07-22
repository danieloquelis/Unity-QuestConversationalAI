using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PcmAudioPlayer : MonoBehaviour
{
    private Queue<AudioClip> clipQueue = new Queue<AudioClip>();
    private AudioSource audioSource;
    private const int SampleRate = 16000;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!audioSource.isPlaying && clipQueue.Count > 0)
        {
            audioSource.clip = clipQueue.Dequeue();
            audioSource.Play();
        }
    }

    public void EnqueueBase64Audio(string base64Audio)
    {
        byte[] audioBytes = System.Convert.FromBase64String(base64Audio);
        int samples = audioBytes.Length / 2;
        float[] floatSamples = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            short sample = (short)((audioBytes[i * 2 + 1] << 8) | audioBytes[i * 2]);
            floatSamples[i] = sample / 32768f;
        }

        AudioClip clip = AudioClip.Create("ElevenLabsClip", samples, 1, SampleRate, false);
        clip.SetData(floatSamples, 0);
        clipQueue.Enqueue(clip);
    }

    public void Stop()
    {
        clipQueue.Clear();
        audioSource.Stop();
    }

    public bool IsPlaying => audioSource.isPlaying || clipQueue.Count > 0;
}