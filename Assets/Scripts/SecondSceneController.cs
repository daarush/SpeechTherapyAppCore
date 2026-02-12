using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SecondSceneController : MonoBehaviour
{
    public TMP_Text wordText;
    public TMP_Text phonemeText;
    public TMP_Text vowelText;
    public TMP_Text lengthText;

    void Start()
    {
    string word = PlayerPrefs.GetString("WORD", "");

    // Original weights
    float phonemeOrig = PlayerPrefs.GetFloat("PHONEME_ORIG", 0f);
    float vowelOrig   = PlayerPrefs.GetFloat("VOWEL_ORIG", 0f);
    float lengthOrig  = PlayerPrefs.GetFloat("LENGTH_ORIG", 0f);

    // Weighted results
    float phonemeWeighted = PlayerPrefs.GetFloat("PHONEME_WEIGHTED", 0f);
    float vowelWeighted   = PlayerPrefs.GetFloat("VOWEL_WEIGHTED", 0f);
    float lengthWeighted  = PlayerPrefs.GetFloat("LENGTH_WEIGHTED", 0f);

    wordText.text = $"Word: {word}";
    phonemeText.text = $"Phoneme: {phonemeOrig:F0} → {phonemeWeighted:F1}";
    vowelText.text   = $"Vowel: {vowelOrig:F0} → {vowelWeighted:F1}";
    lengthText.text  = $"Length: {lengthOrig:F0} → {lengthWeighted:F1}";
    }

    public void OnClearPressed()
    {
        PlayerPrefs.DeleteKey("WORD");
        PlayerPrefs.DeleteKey("PHONEME");
        PlayerPrefs.DeleteKey("VOWEL");
        PlayerPrefs.DeleteKey("LENGTH");

        SceneManager.LoadScene("FirstScene");
    }
}