using System.Collections.Generic;
using UnityEngine;

public class PuzzleVisualManager : MonoBehaviour
{
    public static PuzzleVisualManager Instance;

    [Header("Full Puzzle Prefab")]
    public GameObject fullPuzzlePrefab;

    // ����� �� �� ������ ������ ���� ��Prefab
    private Dictionary<int, GameObject> fullPuzzlePieces = new Dictionary<int, GameObject>();

    // ����� ��� ���� ����� ��� ������
    private HashSet<int> connectedPieces = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (fullPuzzlePrefab != null)
        {
            fullPuzzlePrefab.SetActive(false); // ������� �����

            // ������ �� �� ������ ������� ��� ID
            foreach (Transform child in fullPuzzlePrefab.transform)
            {
                if (child.name.StartsWith("Piece_"))
                {
                    string idStr = child.name.Replace("Piece_", "");
                    if (int.TryParse(idStr, out int id))
                    {
                        fullPuzzlePieces[id] = child.gameObject;
                        child.gameObject.SetActive(false); // ������ �����
                    }
                }
            }
        }
    }

    public void ShowConnectedPiece(int pieceID)
    {
        if (!fullPuzzlePieces.ContainsKey(pieceID))
        {
            Debug.LogWarning($"No piece found in full puzzle with ID {pieceID}");
            return;
        }

        // �� �� ���� ������� �������� ? ������� �� ��Prefab
        if (!fullPuzzlePrefab.activeSelf)
            fullPuzzlePrefab.SetActive(true);

        // ������ �� ���� ��� ����� ����
        fullPuzzlePieces[pieceID].SetActive(true);

        // ������� ���� ������ ������ ��������
        connectedPieces.Add(pieceID);
    }
}
