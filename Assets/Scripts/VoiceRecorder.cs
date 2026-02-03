using UnityEngine;
using System.IO;

public class VoiceRecorder : MonoBehaviour
{
    public static VoiceRecorder Instance;

    private AudioClip recording;
    private string micName;
    private bool isRecording = false;
    private float recordingDuration = 5f;
    private const string TEMP_FILENAME = "temp_recording.wav";

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeMicrophone();
    }

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0];
            Debug.Log("Using microphone: " + micName);
        }
        else
        {
            Debug.LogError("No microphone detected!");
        }
    }

    public void SetRecordingDuration(float duration)
    {
        recordingDuration = duration;
    }

    public void StartRecording()
    {
        if (isRecording || micName == null)
        {
            Debug.LogWarning("Cannot start recording - already recording or no microphone detected");
            return;
        }

        isRecording = true;
        recording = Microphone.Start(micName, false, (int)recordingDuration + 1, 44100);
        Debug.Log($"Recording started for {recordingDuration} seconds...");
    }

    public void StopAndSaveRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("No recording in progress");
            return;
        }

        int position = Microphone.GetPosition(micName);
        Microphone.End(micName);
        isRecording = false;

        // Trim the audio clip to actual recorded length
        float[] samples = new float[position * recording.channels];
        recording.GetData(samples, 0);
        
        AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", position, recording.channels, recording.frequency, false);
        trimmedClip.SetData(samples, 0);

        Debug.Log("Recording stopped and saved.");

        // Save to temp file (overwrites previous)
        SaveWav(TEMP_FILENAME, trimmedClip);
    }

    private void SaveWav(string filename, AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] wavData = ConvertToWav(samples, clip.channels, clip.frequency);
        string filePath = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(filePath, wavData);

        Debug.Log($"Saved recording to: {filePath}");
    }

    private byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int byteRate = sampleRate * channels * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(samples.Length * 2);

            foreach (float sample in samples)
            {
                short val = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(val);
            }

            return stream.ToArray();
        }
    }

    // Public method to get the path of the latest recording
    public string GetLatestRecordingPath()
    {
        return Path.Combine(Application.persistentDataPath, TEMP_FILENAME);
    }

    public bool IsCurrentlyRecording()
    {
        return isRecording;
    }
}