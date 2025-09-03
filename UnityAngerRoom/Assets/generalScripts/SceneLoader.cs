using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    public string sceneName = "menuScene";

    public void LoadScene()
    {
        if (ScreenFader.Instance != null)
        {
            // פייד מסודר עם טעינה
            ScreenFader.Instance.FadeToScene(sceneName);
        }
        else
        {
            // אם משום מה אין Fader בסצינה, נ fallback לטעינה רגילה
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}