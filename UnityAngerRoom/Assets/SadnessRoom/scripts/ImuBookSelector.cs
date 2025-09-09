using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BookBinding
{
    public string id;                   // "Blue", "Orange", ...
    public Transform flashlight;        // שורש הספר (עם Rigidbody)
    public Transform gripPoint;         // נקודת אחיזה (אם ריק -> flashlight)
    public Transform floorAnchor;       // Anchor על הרצפה
    public Vector3 rotationOffsetEuler; // אופציונלי
}

public class ImuBookSelector : MonoBehaviour
{
    [Header("Sensor")]
    public EspImuReaderSad sensor;

    [Header("Books")]
    public List<BookBinding> books = new List<BookBinding>();

    [Header("Flow Options")]
    public bool unlockOnAnySelection = false;
    public string unlockOnlyWhenId = "Blue"; // רק כחול פותח כברירת מחדל
    public bool snapToAnchorOnSelect = true;

    [Header("UI")]
    [Tooltip("שורש קאנבס הכפתורים שיש לכבות אחרי בחירה")]
    public GameObject buttonsCanvasRoot;
    [Tooltip("לכבות את קאנבס הכפתורים בכל בחירה")]
    public bool hideCanvasOnSelect = true;

    [Header("Debug")]
    public bool verbose = true;

    int _currentIndex = -1;
    string _tag => $"[ImuBookSelector:{name}]";

    void Awake()
    {
        if (sensor == null)
            Debug.LogError($"{_tag} Sensor reference is NULL – לא יחובר כלום בלחיצה.");
        if (books == null || books.Count == 0)
            Debug.LogWarning($"{_tag} books list is empty – אין מה לבחור.");
    }

    void OnValidate()
    {
        // בדיקה מהירה בזמן עריכה
        if (books != null)
        {
            for (int i = 0; i < books.Count; i++)
            {
                var b = books[i];
                if (b.flashlight == null)
                    Debug.LogWarning($"{_tag} Book[{i}] id='{b.id}' אין flashlight (Transform).");
            }
        }
    }

    // לקרוא מכפתור – לפי אינדקס
    public void SelectByIndex(int idx)
    {
        if (verbose) Debug.Log($"{_tag} SelectByIndex({idx}) pressed.");
        if (sensor == null) { Debug.LogError($"{_tag} sensor is NULL."); return; }
        if (books == null || books.Count == 0) { Debug.LogError($"{_tag} books list empty."); return; }
        if (idx < 0 || idx >= books.Count) { Debug.LogError($"{_tag} index {idx} out of range [0..{books.Count - 1}]."); return; }
        DoBind(idx);
    }

    // לקרוא מכפתור – לפי מזהה טקסטואלי (Blue, Red...)
    public void SelectById(string id)
    {
        Debug.Log("entering selectbyid");
        if (verbose) Debug.Log($"{_tag} SelectById('{id}') pressed.");
        if (sensor == null) { Debug.LogError($"{_tag} sensor is NULL."); return; }
        if (string.IsNullOrWhiteSpace(id)) { Debug.LogError($"{_tag} id is null/empty."); return; }
        if (books == null || books.Count == 0) { Debug.LogError($"{_tag} books list empty."); return; }

        int idx = books.FindIndex(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            // הדפסה של מה כן יש
            string available = string.Join(", ", books.ConvertAll(b => $"'{b.id}'"));
            Debug.LogError($"{_tag} no book with id '{id}'. Available ids: {available}");
            return;
        }
        DoBind(idx);
    }

    void DoBind(int idx)
    {
        Debug.Log("entering dobind");
        var b = books[idx];
        if (b.flashlight == null)
        {
            Debug.LogError($"{_tag} Book[{idx}] id='{b.id}' has NULL flashlight – לא ניתן לחבר.");
            return;
        }

        if (verbose)
        {
            Debug.Log($"{_tag} DoBind -> idx={idx}, id='{b.id}', " +
                      $"flashlight='{b.flashlight.name}', grip='{(b.gripPoint ? b.gripPoint.name : "NULL->use flashlight")}', " +
                      $"floorAnchor='{(b.floorAnchor ? b.floorAnchor.name : "NULL")}'");
        }

        // אם הספר היה נעול בבית – לשחרר לפני החיבור
        var home = b.flashlight.GetComponent<BookHome>();
        if (home && home.IsLockedAtHome)
        {
            if (verbose) Debug.Log($"{_tag} BookHome locked -> UnlockFromHome()");
            home.UnlockFromHome();
        }

        var grip = b.gripPoint ? b.gripPoint : b.flashlight;

        bool unlock =
            unlockOnAnySelection ||
            string.Equals(b.id, unlockOnlyWhenId, StringComparison.OrdinalIgnoreCase);

        if (verbose)
        {
            Debug.Log($"{_tag} Calling sensor.BindToTargets(unlock={unlock}, snap={snapToAnchorOnSelect})");
        }

        sensor.BindToTargets(b.flashlight, grip, b.floorAnchor, unlock, snapToAnchorOnSelect);

        // אם יש תמיכה באופסט רוטציה בסנסור:
        // sensor.bookRotationOffsetEuler = b.rotationOffsetEuler;

        _currentIndex = idx;

        if (hideCanvasOnSelect && buttonsCanvasRoot)
        {
            if (verbose) Debug.Log($"{_tag} Hiding buttons canvas '{buttonsCanvasRoot.name}'");
            buttonsCanvasRoot.SetActive(false);
        }
    }

    // עזרים לבדיקה ידנית מה-Inpector (קליק ימני על הקומפוננט)
    [ContextMenu("Debug/Print Books")]
    void DebugPrintBooks()
    {
        if (books == null) { Debug.Log($"{_tag} books=null"); return; }
        for (int i = 0; i < books.Count; i++)
        {
            var b = books[i];
            Debug.Log($"{_tag} [{i}] id='{b.id}', flash='{(b.flashlight?b.flashlight.name:"NULL")}', " +
                      $"grip='{(b.gripPoint?b.gripPoint.name:"NULL")}', floor='{(b.floorAnchor?b.floorAnchor.name:"NULL")}'");
        }
    }
}
