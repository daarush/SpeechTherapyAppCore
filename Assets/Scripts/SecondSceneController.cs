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
        float phoneme = PlayerPrefs.GetFloat("PHONEME", 0f);
        float vowel = PlayerPrefs.GetFloat("VOWEL", 0f);
        float length = PlayerPrefs.GetFloat("LENGTH", 0f);

        wordText.text = $"Word: {word}";
        phonemeText.text = $"Phoneme: {phoneme:0.##}";
        vowelText.text = $"Vowel: {vowel:0.##}";
        lengthText.text = $"Length: {length:0.##}";
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
