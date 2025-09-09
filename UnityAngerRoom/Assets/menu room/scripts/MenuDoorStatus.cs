using UnityEngine;

public class MenuDoorStatus : MonoBehaviour
{
    [Tooltip("מזהה ייחודי לחדר (למשל 'BookRoom', 'JoyRoom' וכו')")]
    public string roomId;

    [Tooltip("CanvasGroup / GameObject שמראה הודעה 'You already completed this room'")]
    public GameObject completedCanvas;

    void Start()
    {
        UpdateStatus();
    }

    void OnEnable()
    {
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        if (!completedCanvas) return;

        bool done = RoomProgressManager.Instance != null &&
                    RoomProgressManager.Instance.IsRoomCompleted(roomId);

        completedCanvas.SetActive(done);
    }
}