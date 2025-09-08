using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// מנהל חדר גנרי (סינגלטון מקומי לסצנה).
/// שם על אובייקט ריק בשם "RoomManager" בכל סצנת חדר.
/// הלוגיקה של החדר קוראת CompleteMission / SetMissionState / ResetMission.
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("State (read-only)")]
    [SerializeField] private bool missionCompleted = false;
    
    //public bool MissionCompleted => missionCompleted;

    //[Header("Events")]
    //public UnityEvent OnMissionCompleted;
    //public UnityEvent OnMissionReset;
    //[System.Serializable] public class BoolEvent : UnityEvent<bool> { }
    //public BoolEvent OnMissionStateChanged;

    void Awake()
    {
        // סינגלטון מקומי לסצנה (לא DontDestroyOnLoad!)
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary> מסמנת שהמשימה הושלמה (אם לא הושלמה כבר). </summary>
    public void CompleteMission()
    {
        missionCompleted = true;
    }

    public bool IsMissionCompleted()
    {
        return missionCompleted;
    }


    /// <summary> מאפסת את המשימה בחדר. </summary>
    public void ResetMission()
    {
        bool wasCompleted = missionCompleted;
        missionCompleted = false;
        //OnMissionStateChanged?.Invoke(missionCompleted);
        //OnMissionReset?.Invoke();
    }
}
