using UnityEngine;

public class NewGameStarter : MonoBehaviour
{
    public void OnNewGame()
    {
        if (RoomRunManager.Instance != null)
        {
            RoomRunManager.Instance.StartNewRun(); // מגריל חדר ראשון ומטעין אותו עם פייד
        }
        else
        {
            Debug.LogError("[NewGameStarter] No RoomRunManager in the menu scene.");
        }
    }
}