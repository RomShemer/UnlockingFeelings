using UnityEngine;

public class MenuController : MonoBehaviour
{
    public void ResetRunStats()
    {
        if (RunStats.Instance != null)
            RunStats.Instance.ResetAll();
    }
}