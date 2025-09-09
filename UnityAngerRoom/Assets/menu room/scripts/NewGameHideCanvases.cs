using UnityEngine;

public class NewGameHideCanvases : MonoBehaviour
{
    [Tooltip("כל הקנבסים/אובייקטים שצריכים להיעלם בלחיצה על New Game")]
    public GameObject[] canvasesToHide;

    public void OnNewGame()
    {
        foreach (var go in canvasesToHide)
            if (go) go.SetActive(false);
    }
}