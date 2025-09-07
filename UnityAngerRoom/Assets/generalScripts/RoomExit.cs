using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomExit : MonoBehaviour
{
    [Tooltip("תג של השחקן (למשל Player). ריק = כל כניסה תפעיל.")]
    public string playerTag = "Player";

    bool fired = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        fired = true;                       // מונע כפילות
        if (ScreenFader.Instance == null)
            Debug.LogWarning("[RoomExit] ScreenFader.Instance == null בזמן טריגר (ניפול לטעינה בלי פייד אם המנהל לא ייצור אחד).");

        if (RoomRunManager.Instance != null)
            RoomRunManager.Instance.LoadNextRoom(); // המנהל כבר עושה FadeToScene
        else
            Debug.LogError("[RoomExit] No RoomRunManager in scene.");
    }
}