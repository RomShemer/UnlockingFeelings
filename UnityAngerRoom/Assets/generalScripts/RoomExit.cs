using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomExit : MonoBehaviour
{
    [Tooltip("תג של השחקן (למשל Player). ריק = כל כניסה תפעיל.")]
    public string playerTag = "Default";

    [Tooltip("מזהה החדר עבור התפריט (למשל BookRoom, JoyRoom, AngerRoom...)")]
    public string roomId = "AngerRoom";

    bool fired = false;
    public RoomManager roomManager;

    public void Start()
    {
        if (roomManager == null)
        {
            roomManager = RoomManager.Instance;
            if (roomManager == null)
                Debug.LogWarning("[RoomExit] No RoomManager instance found in scene.");
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (roomManager != null && roomManager.IsMissionCompleted())
        {
            Debug.Log($"[RoomExit] Trigger enter by {other.name} (tag={other.tag})");

            fired = true;
            Debug.Log("[RoomExit] Trigger accepted, processing exit...");

            // --- חדש: רושם שהחדר הנוכחי הושלם ---
            if (RunStats.Instance != null)
            {
                RunStats.Instance.CompleteCurrent("exit");
                Debug.Log("[RoomExit] Marked current room as completed in RunStats.");
            }
            else
            {
                Debug.LogWarning("[RoomExit] RunStats.Instance == null (no stats recorded).");
            }

            if (RoomRunManager.Instance != null)
            {
                Debug.Log("[RoomExit] Calling RoomRunManager.LoadMenu()");

                // --- חדש: סימון החדר כהושלם עבור התפריט ---
                if (RoomProgressManager.Instance != null && !string.IsNullOrEmpty(roomId))
                {
                    RoomProgressManager.Instance.MarkRoomCompleted(roomId);
                    Debug.Log($"[RoomExit] Marked room '{roomId}' as completed.");
                }

                RoomRunManager.Instance.LoadMenu(); // המנהל כבר עושה FadeToScene
            }
            else
            {
                Debug.LogError("[RoomExit] No RoomRunManager in scene.");
            }
        }
        else if (roomManager == null)
        {
            Debug.Log($"[RoomExit] Trigger enter by {other.name} (tag={other.tag})");

            if (fired)
            {
                Debug.Log("[RoomExit] Already fired once, ignoring.");
                return;
            }

            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            {
                Debug.Log($"[RoomExit] Ignored because tag mismatch (expected={playerTag}).");
                return;
            }

            fired = true;
            Debug.Log("[RoomExit] Trigger accepted, processing exit...");

            // --- חדש: רושם שהחדר הנוכחי הושלם ---
            if (RunStats.Instance != null)
            {
                RunStats.Instance.CompleteCurrent("exit");
                Debug.Log("[RoomExit] Marked current room as completed in RunStats.");
            }
            else
            {
                Debug.LogWarning("[RoomExit] RunStats.Instance == null (no stats recorded).");
            }

            if (RoomRunManager.Instance != null)
            {
                Debug.Log("[RoomExit] Calling RoomRunManager.LoadMenu()");

                // --- חדש: סימון החדר כהושלם עבור התפריט ---
                if (RoomProgressManager.Instance != null && !string.IsNullOrEmpty(roomId))
                {
                    RoomProgressManager.Instance.MarkRoomCompleted(roomId);
                    Debug.Log($"[RoomExit] Marked room '{roomId}' as completed.");
                }

                RoomRunManager.Instance.LoadMenu(); // המנהל כבר עושה FadeToScene
            }
            else
            {
                Debug.LogError("[RoomExit] No RoomRunManager in scene.");
            }
        }
    }
}
