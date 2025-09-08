using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Tooltip("כמה התאמות נדרשות (אצלך 6)")]
    public int totalGoals = 6;

    [Tooltip("בקר הדלת – ראה DoorController למטה")]
    public DoorController door;

    int placedCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ReportPlaced()
    {
        placedCount++;
        if (placedCount >= totalGoals && door != null)
            door.Open();
    }
}