using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PuzzleConnection
{
    public int pieceID;
    public List<int> connectableIDs;
}

public class PuzzleConnections : MonoBehaviour
{
    [Header("Puzzle Connections Settings")]
    public List<PuzzleConnection> connections = new List<PuzzleConnection>();

    private Dictionary<int, List<int>> connectionMap = new Dictionary<int, List<int>>();

    private void Awake()
    {
        connectionMap.Clear();
        foreach (var c in connections)
        {
            connectionMap[c.pieceID] = c.connectableIDs;
        }
    }

    public bool CanConnect(int fromID, int toID)
    {
        return connectionMap.ContainsKey(fromID) && connectionMap[fromID].Contains(toID);
    }

    public List<int> GetConnections(int pieceID)
    {
        if (connectionMap.ContainsKey(pieceID))
            return connectionMap[pieceID];

        return new List<int>();
    }
}
