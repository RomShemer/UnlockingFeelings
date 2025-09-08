using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ���� ��� ���� (�������� ����� �����).
/// �� �� ������� ��� ��� "RoomManager" ��� ���� ���.
/// ������� �� ���� ����� CompleteMission / SetMissionState / ResetMission.
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
        // �������� ����� ����� (�� DontDestroyOnLoad!)
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary> ����� ������� ������ (�� �� ������ ���). </summary>
    public void CompleteMission()
    {
        missionCompleted = true;
    }

    public bool IsMissionCompleted()
    {
        return missionCompleted;
    }


    /// <summary> ����� �� ������ ����. </summary>
    public void ResetMission()
    {
        bool wasCompleted = missionCompleted;
        missionCompleted = false;
        //OnMissionStateChanged?.Invoke(missionCompleted);
        //OnMissionReset?.Invoke();
    }
}
