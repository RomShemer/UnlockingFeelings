using UnityEngine;

public class PauseMenuButtons : MonoBehaviour
{
    public void OnResume() { VRPauseManager.Instance?.Resume(); }
    public void OnQuit() { VRPauseManager.Instance?.QuitToMenu(); }
}
