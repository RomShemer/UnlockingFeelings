using UnityEngine;
using System.Collections;

public class BookSocketTrigger : MonoBehaviour
{
    [Header("Which objects count as closed books")]
    public LayerMask bookLayers;

    [Header("Open books on the piano")]
    public GameObject openBookCorrect;        // OpenBook_Correct
    public GameObject openBookWrong;          // OpenBook_Wrong

    [Header("Behaviour")]
    public string sadnessTag = "SadnessBook"; // תג של הספר הכחול
    public float wrongDisplaySeconds = 5f;

    [Header("Sensor")]
    public EspImuReaderSad sensor;            // גרור את החיישן

    [Header("UI")]
    [Tooltip("שורש קאנבס הכפתורים שנרצה להדליק רק לאחר שספר שגוי חוזר הביתה")]
    public GameObject buttonsCanvasRoot;
    [Tooltip("להדליק את קאנבס הכפתורים אחרי שהספר השגוי חוזר לביתו")]
    public bool showButtonsOnWrongReturn = true;

    Coroutine wrongUiRoutine;

    void OnTriggerEnter(Collider other)
    {
        if ((bookLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        var closedBook = rb.gameObject;

        // מכבים את הספר הסגור ששמו בסוקט
        closedBook.SetActive(false);

        bool isSadness = closedBook.CompareTag(sadnessTag);

        if (isSadness)
        {
            // נכון: השאר את הספר הפתוח על הפסנתר; אל תחזיר קאנבס
            if (wrongUiRoutine != null) { StopCoroutine(wrongUiRoutine); wrongUiRoutine = null; }
            if (openBookWrong)   openBookWrong.SetActive(false);
            if (openBookCorrect) openBookCorrect.SetActive(true);
        }
        else
        {
            // שגוי: הצג Wrong, ואז החזר, נעל בבית, נתק חיישן, והחזר קאנבס כפתורים
            if (openBookCorrect) openBookCorrect.SetActive(false);
            if (openBookWrong)   openBookWrong.SetActive(true);

            if (wrongUiRoutine != null) StopCoroutine(wrongUiRoutine);
            wrongUiRoutine = StartCoroutine(HideWrongUiAfterDelay());

            StartCoroutine(ReturnWrongBookAfterDelay(closedBook));
        }
    }

    IEnumerator HideWrongUiAfterDelay()
    {
        yield return new WaitForSeconds(wrongDisplaySeconds);
        if (openBookWrong) openBookWrong.SetActive(false);
        wrongUiRoutine = null;
    }

    IEnumerator ReturnWrongBookAfterDelay(GameObject closedBook)
    {
        yield return new WaitForSeconds(wrongDisplaySeconds);

        var home = closedBook.GetComponent<BookHome>();
        if (home && home.bookPlace)
        {
            // החזרה מיידית ונעילה קבועה בבית
            home.LockToHomeNow();
        }
        else
        {
            // Fallback: הקפאה במקום אם אין BookHome
            var rb = closedBook.GetComponent<Rigidbody>();
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        // הדלק ספר (כבר בבית ובנעילה)
        if (!closedBook.activeSelf) closedBook.SetActive(true);

        // אם החיישן היה מחובר בדיוק לספר הזה – נתק ודרוש בחירה מחדש
        if (sensor && sensor.CurrentFlashlight == closedBook.transform)
            sensor.ClearBinding(relock: true);

        // קאנבס הכפתורים חוזר רק אחרי טעות
        if (showButtonsOnWrongReturn && buttonsCanvasRoot)
            buttonsCanvasRoot.SetActive(true);

        // ליתר ביטחון – לכבות Wrong אם עוד דולק
        if (openBookWrong) openBookWrong.SetActive(false);
    }
}
