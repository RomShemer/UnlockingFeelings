    using UnityEngine;
    using System.Collections.Generic;

    [CreateAssetMenu(fileName = "PuzzleConnectionsConfig", menuName = "Puzzle/ConnectionsConfig")]
    public class PuzzleConnectionsConfig : ScriptableObject
    {
        [System.Serializable]
        public class Connection
        {
            public int pieceA;
            public int pieceB;
        }

        public List<Connection> allowedConnections = new List<Connection>();

        public bool CanConnect(int idA, int idB)
        {
            Debug.Log("entering can connect");

            foreach (var c in allowedConnections)
            {
                if ((c.pieceA == idA && c.pieceB == idB) || (c.pieceA == idB && c.pieceB == idA))
                {
                    Debug.Log("can connect");
                    return true;

                }
            }

            Debug.Log("can not connect");
            return false;
        }

        public bool isAllConnected(int numOfConnections)
        {
            return allowedConnections.Count == numOfConnections;
        }

    }
