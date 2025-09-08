using System.Collections.Generic;
using UnityEngine;

public class PuzzleVisualManager : MonoBehaviour
{
    public static PuzzleVisualManager Instance;

    [Header("Full Puzzle Prefab")]
    public GameObject fullPuzzlePrefab;

    // רשימה של כל החלקים המלאים בתוך ה־Prefab
    private Dictionary<int, GameObject> fullPuzzlePieces = new Dictionary<int, GameObject>();

    // שמירת מצב אילו חלקים כבר התחברו
    private HashSet<int> connectedPieces = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (fullPuzzlePrefab != null)
        {
            fullPuzzlePrefab.SetActive(false); // מתחילים מוסתר

            // אוספים את כל הילדים ומסדרים לפי ID
            foreach (Transform child in fullPuzzlePrefab.transform)
            {
                if (child.name.StartsWith("Piece_"))
                {
                    string idStr = child.name.Replace("Piece_", "");
                    if (int.TryParse(idStr, out int id))
                    {
                        fullPuzzlePieces[id] = child.gameObject;
                        child.gameObject.SetActive(false); // בהתחלה מוסתר
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

        // אם זו הפעם הראשונה שמתחברים ? מפעילים את ה־Prefab
        if (!fullPuzzlePrefab.activeSelf)
            fullPuzzlePrefab.SetActive(true);

        // מציגים את החלק הזה בפאזל המלא
        fullPuzzlePieces[pieceID].SetActive(true);

        // מוסיפים אותו לרשימת החלקים המחוברים
        connectedPieces.Add(pieceID);
    }
}
