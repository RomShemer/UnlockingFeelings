using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneWithFade(string sceneName)
    {
        if (ScreenFader.Instance != null)
            StartCoroutine(LoadRoutine(sceneName));
        else
            SceneManager.LoadScene(sceneName); // גיבוי
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        yield return ScreenFader.Instance.FadeOut();            // פייד־אאוט
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        // בסצנה החדשה ה-ScreenFader עושה פייד־אין אוטומטי (Start)
    }

    // נוח: פונקציה ייעודית ל-New Game
    public void StartNewGame() => LoadSceneWithFade("JoyScene 1");

	public void BackToMenu()
	{
    	LoadSceneWithFade("menuScene"); // השם חייב להיות בדיוק כמו ברשימת Build Settings
	}

}