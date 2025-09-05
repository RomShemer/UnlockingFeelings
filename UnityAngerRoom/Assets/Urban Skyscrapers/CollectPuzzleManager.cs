using UnityEngine;
using UnityEngine.SceneManagement;

public class CollectPuzzleManager : MonoBehaviour
{
    [Header("UI")]
    public ProgressBG progressBarUI;
    private int totalPieces = 16;
    private int countPieces = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        progressBarUI.Init(16); // 16 �����

    }

    public void ReportConnection()
    {
        countPieces++;
        progressBarUI?.ReportOne();

        //if (countPieces >= totalPieces)
        //{
        //    Debug.Log("all puzzles connected");
        //    OnPuzzleComplete();
        //}
    }

    //private void OnPuzzleComplete()
    //{
    //    Debug.Log("finish puzzel- open door");
    //    //door?.OpenDoor();  // ����� ������
    //}
    
    public bool isCollectAllPuzzles()
    {
        return countPieces >= totalPieces;
    }
}
