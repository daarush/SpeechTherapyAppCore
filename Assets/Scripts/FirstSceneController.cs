using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Globalization;
using System.Collections;

public class FirstSceneController : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField wordInput;
    public TMP_InputField phonemeInput;
    public TMP_InputField vowelInput;
    public TMP_InputField lengthInput;

    [Header("Output")]
    public TMP_Text outputText;  // Reference to Output/Scroll View/Content TextMeshPro - Text (UI) component

    [Header("Recording Settings")]
    public float recordingDuration = 5f;

    private bool isProcessing = false;

    void Start()
    {
        // Set default values (70, 15, 15)
        if (phonemeInput != null) phonemeInput.text = "70";
        if (vowelInput != null) vowelInput.text = "15";
        if (lengthInput != null) lengthInput.text = "15";
    }

    public void OnStartPressed()
    {
        if (isProcessing)
        {
            Debug.LogWarning("Already processing a recording!");
            return;
        }

        // Get input values (0-100 scale, divide by 100)
        string targetWord = wordInput.text.Trim();
        float phonemeWeight = ParseFloat(phonemeInput) / 100f;
        float vowelWeight = ParseFloat(vowelInput) / 100f;
        float lengthWeight = ParseFloat(lengthInput) / 100f;

        // Validate inputs
        if (string.IsNullOrEmpty(targetWord))
        {
            AppendOutput("ERROR: Please enter a word!");
            return;
        }

        // Update scoring weights
        PhonemeScoringEngine.SetWeights(phonemeWeight, vowelWeight, lengthWeight);

        // Start the recording and processing pipeline
        StartCoroutine(RecordAndProcessPipeline(targetWord, phonemeWeight, vowelWeight, lengthWeight));
    }

    private IEnumerator RecordAndProcessPipeline(string targetWord, float phonemeWeight, float vowelWeight, float lengthWeight)
    {
        isProcessing = true;
        float startTime = Time.time;

        // Check if VoiceRecorder exists
        if (VoiceRecorder.Instance == null)
        {
            AppendOutput("ERROR: VoiceRecorder not found in scene!");
            isProcessing = false;
            yield break;
        }

        // Check if Wav2VecManager exists
        if (Wav2VecManager.Instance == null)
        {
            AppendOutput("ERROR: Wav2VecManager not found in scene!");
            isProcessing = false;
            yield break;
        }

        AppendOutput("Recording started...");

        // Set recording duration and start recording
        VoiceRecorder.Instance.SetRecordingDuration(recordingDuration);
        VoiceRecorder.Instance.StartRecording();
        
        // Wait for recording duration
        yield return new WaitForSeconds(recordingDuration);

        // Stop recording and save
        VoiceRecorder.Instance.StopAndSaveRecording();
        AppendOutput("Recording stopped. Processing...");

        // Get the path to the latest recording
        string recordingPath = VoiceRecorder.Instance.GetLatestRecordingPath();

        // Convert target word to phonemes
        string targetPhonemes = PhonemeConverter.ConvertWordAsString(targetWord);

        if (string.IsNullOrEmpty(targetPhonemes))
        {
            AppendOutput($"ERROR: Could not find phonemes for word '{targetWord}' in CMU dictionary!");
            isProcessing = false;
            yield break;
        }

        // Process audio and get score
        bool scoreReceived = false;
        float finalScore = 0f;
        string spokenPhonemes = "";

        Wav2VecManager.Instance.GetScoreFromFile(recordingPath, targetWord, (score, spoken, target) =>
        {
            finalScore = score;
            spokenPhonemes = spoken;
            scoreReceived = true;
        });

        // Wait for score calculation
        float timeout = 30f;
        float elapsed = 0f;
        while (!scoreReceived && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (!scoreReceived)
        {
            AppendOutput("ERROR: Timeout waiting for score calculation!");
            isProcessing = false;
            yield break;
        }

        // Calculate total processing time
        float totalTime = Time.time - startTime;

        // Format and display results (show original 0-100 scale values)
        string resultText = $"\n___________\n" +
                           $"Chosen Word: {targetWord}\n" +
                           $"Converted Target Word to phonemes: [{targetPhonemes}]\n" +
                           $"Spoken word to phonemes: [{spokenPhonemes}]\n" +
                           $"PHONEME_EDIT_WEIGHT = {phonemeWeight * 100f:F0}\n" +
                           $"VOWEL_WEIGHT = {vowelWeight * 100f:F0}\n" +
                           $"LENGTH_WEIGHT = {lengthWeight * 100f:F0}\n" +
                           $"Total Score (out of 100) = {finalScore:F2}%\n" +
                           $"Total Time: {totalTime:F1}s\n" +
                           $"___________\n";

        AppendOutput(resultText);

        isProcessing = false;
    }

    private void AppendOutput(string text)
    {
        if (outputText != null)
        {
            outputText.text += text + "\n";
        }
        Debug.Log(text);
    }

    private float ParseFloat(TMP_InputField input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return 0f;

        float.TryParse(
            input.text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float value
        );
        return value;
    }

    public void OnClearOutputPressed()
    {
        if (outputText != null)
        {
            outputText.text = "";
        }
    }
}