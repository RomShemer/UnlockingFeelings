using System.Collections.Generic;
using UnityEngine;

public class RoomProgressManager : MonoBehaviour
{
    public static RoomProgressManager Instance;

    // שומר אילו חדרים הושלמו
    private HashSet<string> completedRooms = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // נשאר בין סצנות
    }

    public void MarkRoomCompleted(string roomId)
    {
        completedRooms.Add(roomId);
    }

    public bool IsRoomCompleted(string roomId)
    {
        return completedRooms.Contains(roomId);
    }
}