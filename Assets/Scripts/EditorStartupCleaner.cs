using UnityEngine;

public static class EditorStartupCleaner
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ClearLogOnPlay()
    {
        PlayerPrefs.DeleteKey("OUTPUT_LOG");
        PlayerPrefs.Save();
        Debug.Log("Cleared OUTPUT_LOG at Play start.");
    }
#endif
}