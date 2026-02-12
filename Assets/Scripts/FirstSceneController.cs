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
        if (outputText != null) outputText.text = "";
        if (phonemeInput != null) phonemeInput.text = "70";
        if (vowelInput != null) vowelInput.text = "15";
        if (lengthInput != null) lengthInput.text = "15";

        if (outputText != null)
        {
            outputText.text = PlayerPrefs.GetString("OUTPUT_LOG", "");
            StartCoroutine(ScrollToBottom()); // scroll to bottom on scene load
        }
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
        PhonemeScoringEngine.ScoreBreakdown breakdown = default;
        string spokenPhonemes = "";

        Wav2VecManager.Instance.GetScoreFromFile(
            recordingPath,
            targetWord,
            (result, spoken, target) =>
            {
                breakdown = result;
                spokenPhonemes = spoken;
                scoreReceived = true;
            }
        );

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

        //final score access now
        float finalScore = breakdown.total;

        phonemeInput.text = $"{phonemeWeight * 100f:F0} → {breakdown.phonemeWeighted:F1}";
        vowelInput.text   = $"{vowelWeight * 100f:F0} → {breakdown.vowelWeighted:F1}";
        lengthInput.text  = $"{lengthWeight * 100f:F0} → {breakdown.lengthWeighted:F1}";

        // Format and display results (show original 0-100 scale values)
        string resultText = $"\n___________\n" +
                    $"Chosen Word: {targetWord}\n" +
                    $"Converted Target Word to phonemes: [{targetPhonemes}]\n" +
                    $"Spoken word to phonemes: [{spokenPhonemes}]\n\n" +

                    $"PHONEME_EDIT_WEIGHT = {phonemeWeight * 100f:F0}%  →  {breakdown.phonemeWeighted:F1}\n" +
                    $"VOWEL_WEIGHT        = {vowelWeight * 100f:F0}%  →  {breakdown.vowelWeighted:F1}\n" +
                    $"LENGTH_WEIGHT       = {lengthWeight * 100f:F0}%  →  {breakdown.lengthWeighted:F1}\n\n" +

                    $"Total Score (out of 100) = {finalScore:F2}%\n" +
                    $"Total Time: {totalTime:F1}s\n" +
                    $"___________\n";


        AppendOutput(resultText);

        // Save both original weight (0-100) and weighted score
        PlayerPrefs.SetString("WORD", targetWord);

        // Original weights
        PlayerPrefs.SetFloat("PHONEME_ORIG", phonemeWeight * 100f);
        PlayerPrefs.SetFloat("VOWEL_ORIG", vowelWeight * 100f);
        PlayerPrefs.SetFloat("LENGTH_ORIG", lengthWeight * 100f);

        // Weighted results
        PlayerPrefs.SetFloat("PHONEME_WEIGHTED", breakdown.phonemeWeighted);
        PlayerPrefs.SetFloat("VOWEL_WEIGHTED", breakdown.vowelWeighted);
        PlayerPrefs.SetFloat("LENGTH_WEIGHTED", breakdown.lengthWeighted);

        PlayerPrefs.Save();
        isProcessing = false;
        SceneManager.LoadScene("SecondScene");
    }

    private void AppendOutput(string text)
    {
        if (outputText != null)
        {
            outputText.text += text + "\n";
            PlayerPrefs.SetString("OUTPUT_LOG", outputText.text);
            PlayerPrefs.Save();
            StartCoroutine(ScrollToBottom());

            // Scroll to bottom if using a ScrollRect
            var scrollRect = outputText.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f; // scroll to bottom
                Canvas.ForceUpdateCanvases();
            }
        }
        Debug.Log(text);
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null; // wait one frame for TMP text to update

        var scrollRect = outputText.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scrollRect != null)
        {
            // Force TMP to rebuild its layout
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            scrollRect.verticalNormalizedPosition = 0f; // scroll to bottom
            Canvas.ForceUpdateCanvases();
        }
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
            PlayerPrefs.DeleteKey("OUTPUT_LOG");
        }
    }
}