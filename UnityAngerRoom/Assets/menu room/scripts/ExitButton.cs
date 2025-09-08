using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public void ExitGame()
    {
        Debug.Log("Exit pressed - quitting game");
        Application.Quit();

#if UNITY_EDITOR
        // כשאתה מריץ מתוך ה־Editor זה לא באמת יסגור,
        // אז זה יעצור את מצב ה־Play.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}