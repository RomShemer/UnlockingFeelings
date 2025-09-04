using System.Collections.Generic;
using UnityEngine;

public class PuzzleManagerDarkRoom : MonoBehaviour
{
    public static PuzzleManagerDarkRoom Instance { get; private set; }

    [Header("Puzzle Prefab")]
    public GameObject fullPuzzlePrefab;

    [Header("Connections Manager")]
    public PuzzleConnections connectionsManager;

    // ����� �� ������� ������� ����
    private readonly List<PuzzleGroup> activeGroups = new List<PuzzleGroup>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// ���� ����� ���� ���� ����� ������
    /// </summary>
    public PuzzleGroup CreateGroup(MagneticPuzzlePiece pieceA, MagneticPuzzlePiece pieceB)
    {
        GameObject groupObject = new GameObject($"PuzzleGroup_{activeGroups.Count + 1}");
        PuzzleGroup newGroup = groupObject.AddComponent<PuzzleGroup>();
        activeGroups.Add(newGroup);

        newGroup.Initialize(new List<MagneticPuzzlePiece> { pieceA, pieceB });

        //newGroup.AddPiece(pieceA);
        //newGroup.AddPiece(pieceB);

        //activeGroups.Add(newGroup);
        return newGroup;
    }

    /// <summary>
    /// ���� ����� ���� ������
    /// </summary>
    public void RegisterGroup(PuzzleGroup group)
    {
        if (!activeGroups.Contains(group))
            activeGroups.Add(group);
    }

    /// <summary>
    /// ���� ����� ������� ����� �� ���� �����
    /// </summary>
    public void RemoveGroup(PuzzleGroup group)
    {
        if (activeGroups.Contains(group))
            activeGroups.Remove(group);
    }

    public void MergeGroups(PuzzleGroup a, PuzzleGroup b)
    {
        if (a == b) return;

        foreach (var piece in b.pieces)
        {
            a.AddPiece(piece);
        }

        activeGroups.Remove(b);
        Destroy(b.gameObject);
    }
}
