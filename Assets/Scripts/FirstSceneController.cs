using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Globalization;

public class FirstSceneController : MonoBehaviour
{
    public TMP_InputField wordInput;
    public TMP_InputField phonemeInput;
    public TMP_InputField vowelInput;
    public TMP_InputField lengthInput;

    public void OnStartPressed()
    {
        // entering word
        string word = wordInput.text.Trim();

        // numeric inputs
        float phoneme = ParseFloat(phonemeInput);
        float vowel = ParseFloat(vowelInput);
        float length = ParseFloat(lengthInput);

        // save the values
        PlayerPrefs.SetString("WORD", word);
        PlayerPrefs.SetFloat("PHONEME", phoneme);
        PlayerPrefs.SetFloat("VOWEL", vowel);
        PlayerPrefs.SetFloat("LENGTH", length);
        PlayerPrefs.Save();

        SceneManager.LoadScene("SecondScene");
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
}
