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

    int _currentIndex = -1;

    public void SelectByIndex(int idx)
    {
        if (sensor == null || idx < 0 || idx >= books.Count) return;
        DoBind(idx);
    }

    public void SelectById(string id)
    {
        if (sensor == null) return;
        int idx = books.FindIndex(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) DoBind(idx);
    }

    void DoBind(int idx)
    {
        var b = books[idx];
        if (b.flashlight == null) return;

        // אם הספר היה נעול בבית אחרי החזרה – לשחרר את הנעילה לפני החיבור לחיישן
        var home = b.flashlight.GetComponent<BookHome>();
        if (home && home.IsLockedAtHome) home.UnlockFromHome();

        var grip = b.gripPoint ? b.gripPoint : b.flashlight;

        bool unlock =
            unlockOnAnySelection ||
            string.Equals(b.id, unlockOnlyWhenId, StringComparison.OrdinalIgnoreCase);

        sensor.BindToTargets(b.flashlight, grip, b.floorAnchor, unlock, snapToAnchorOnSelect);
        // אם הוספת לסנסור תמיכה ב-offset:
        // sensor.bookRotationOffsetEuler = b.rotationOffsetEuler;

        _currentIndex = idx;

        // לכבות קאנבס כפתורים אחרי בחירה
        if (hideCanvasOnSelect && buttonsCanvasRoot)
            buttonsCanvasRoot.SetActive(false);
    }
}
