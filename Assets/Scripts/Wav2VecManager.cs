using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using Unity.InferenceEngine;

public class Wav2VecManager : MonoBehaviour
{
    public static Wav2VecManager Instance;
    public ModelAsset wav2vecModel;

    void Awake()
    {
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
    }

    void Start()
    {
        var cmuFile = Resources.Load<TextAsset>("cmudict");
        if (cmuFile == null)
        {
            Debug.LogError("Failed to load cmudict from Resources!");
            return;
        }
        PhonemeConverter.LoadCMUDict(cmuFile);
        Debug.Log("CMU Dictionary loaded successfully");
    }

    // Modified function to return score, spoken phonemes, and target phonemes via callback
    public void GetScoreFromFile(
        string recordingPath,
        string targetWord,
        Action<PhonemeScoringEngine.ScoreBreakdown, string, string> onScoreReady
    ) {
    StartCoroutine(LoadClipAndScore(recordingPath, targetWord, onScoreReady));
    }

    private IEnumerator LoadClipAndScore(
        string path,
        string targetWord,
        Action<PhonemeScoringEngine.ScoreBreakdown, string, string> onScoreReady
    )
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load audio: " + www.error);
                onScoreReady?.Invoke(default, "", "");// return -1 on failure
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            var wav2vec = new Wav2VecONNX(wav2vecModel);
            string predictedPhonemes = wav2vec.GetPhonemesFromClip(clip);
            string targetPhonemes = PhonemeConverter.ConvertWordAsString(targetWord);

            Debug.Log($"Target Word: {targetWord}");
            Debug.Log($"Predicted Phonemes: {predictedPhonemes}");
            Debug.Log($"Target Phonemes: {targetPhonemes}");

            var breakdown =  PhonemeScoringEngine.CalculateSimilarityDetailed(
                predictedPhonemes,
                targetPhonemes
            );

            Debug.Log($"Similarity Score: {breakdown.total:F2}");

            onScoreReady?.Invoke(breakdown, predictedPhonemes, targetPhonemes);
            
            // Dispose the wav2vec instance
            wav2vec.Dispose();
        }
    }
}