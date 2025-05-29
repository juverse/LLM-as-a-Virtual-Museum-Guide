using UnityEngine;
using System.IO;
using System.Collections;

public class AudioRecorder : MonoBehaviour
{
    private AudioClip audioClip;
    private string filePath;
    private float startTime; // Time when recording starts
    private int maxLength = 60;

    private int deviceID = 1;

    private bool isRecording = false;
    
    private void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath + "/Assets/Generated", "audioInput.wav");
        filePath = filePath.Replace('\\', '/');
        for (int i = 0; i < Microphone.devices.Length; i++)
        {
            Debug.Log("Device " + i + " " + Microphone.devices[i]);
        }
    }
    // public void StartRecording()
    // {

    //     audioClip = Microphone.Start(Microphone.devices[deviceID], false, maxLength, 24000); // 10 seconds, 44.1kHz
    //     startTime = Time.time; // Start the timer
    //     Debug.Log("Recording started...");
    // }

    // public void StopRecording()
    // {
    //     if (Microphone.IsRecording(Microphone.devices[deviceID]))
    //     {
    //         Microphone.End(Microphone.devices[deviceID]);
    //         float elapsedTime = Time.time - startTime;
    //         TrimAudioClip(elapsedTime); // Trim the audio clip to the actual recorded length
    //         SaveWavFile();
    //         Debug.Log("Recording stopped.");
    //     }
    // }

    public void StartRecording()
    {
        StartCoroutine(StartRecordingCoroutine());
    }

    private IEnumerator StartRecordingCoroutine()
    {
        Debug.Log("Starting microphone...");
        yield return null; // Wait a frame to avoid blocking

        audioClip = Microphone.Start(Microphone.devices[deviceID], false, maxLength, 24000);
        startTime = Time.time;
        isRecording = true;
        Debug.Log("Recording started...");
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            StartCoroutine(StopRecordingCoroutine());
        }
    }

    private IEnumerator StopRecordingCoroutine()
    {
        Debug.Log("Stopping recording...");
        yield return null; // Allow frame update before stopping

        if (Microphone.IsRecording(Microphone.devices[deviceID]))
        {
            Microphone.End(Microphone.devices[deviceID]);
            float elapsedTime = Time.time - startTime;
            TrimAudioClip(elapsedTime); // Trim the audio clip to actual length
            SaveWavFile();
            isRecording = false;
            Debug.Log("Recording stopped.");
        }
    }

    private void TrimAudioClip(float length)
    {
        // Calculate the number of samples to keep
        int sampleCount = Mathf.FloorToInt(length * audioClip.frequency);
        float[] trimmedSamples = new float[sampleCount];

        // Get the original samples
        float[] originalSamples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(originalSamples, 0);

        // Copy only the samples that correspond to the actual recorded length
        for (int i = 0; i < sampleCount * audioClip.channels; i++)
        {
            trimmedSamples[i] = originalSamples[i];
        }

        // Create a new AudioClip with the trimmed samples
        AudioClip trimmedClip = AudioClip.Create(audioClip.name, sampleCount, audioClip.channels, audioClip.frequency, false);
        trimmedClip.SetData(trimmedSamples, 0);

        // Assign the trimmed clip back to the audioClip variable
        audioClip = trimmedClip;
        Debug.Log($"Trimmed audio to {length} seconds.");
    }
    private void SaveWavFile()
    {
        Debug.Log("Env " + Application.persistentDataPath + " " + audioClip);

        SavWav.Save(filePath, audioClip);  // Using a custom SaveWav class to save the AudioClip as a .wav file
        Debug.Log($"Audio saved at: {filePath}");
    }

    public string GetFilePath()
    {
        return filePath;
    }
}
