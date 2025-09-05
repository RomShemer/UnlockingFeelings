using UnityEngine;
using System.Collections;

public class BookSocketTrigger : MonoBehaviour
{
    [Header("Which objects count as closed books")]
    public LayerMask bookLayers;              // סמן כאן את שכבת Book

    [Header("Open books on the piano")]
    public GameObject openBookCorrect;        // גרור את OpenBook_Correct
    public GameObject openBookWrong;          // גרור את OpenBook_Wrong

    [Header("Behaviour")]
    public string sadnessTag = "SadnessBook"; // תג של הספר הכחול
    public float wrongDisplaySeconds = 5f;    // כמה זמן Wrong דולק

    private Coroutine wrongRoutine;

    void OnTriggerEnter(Collider other)
    {
        // מקבלים רק אובייקטים מהשכבות של ספרים
        if ((bookLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        var closedBook = rb.gameObject;

        // לכבות את הספר הסגור ששמו בסוקט
        closedBook.SetActive(false);

        bool isSadness = closedBook.CompareTag(sadnessTag);

        if (isSadness)
        {
            // להציג את הספר הנכון
            if (wrongRoutine != null) { StopCoroutine(wrongRoutine); wrongRoutine = null; }
            if (openBookWrong)   openBookWrong.SetActive(false);
            if (openBookCorrect) openBookCorrect.SetActive(true);
        }
        else
        {
            // להציג את הספר השגוי לזמן קצוב
            if (openBookCorrect) openBookCorrect.SetActive(false);
            if (openBookWrong)
            {
                openBookWrong.SetActive(true);
                if (wrongRoutine != null) StopCoroutine(wrongRoutine);
                wrongRoutine = StartCoroutine(HideWrongAfterDelay());
            }
        }
    }

    IEnumerator HideWrongAfterDelay()
    {
        yield return new WaitForSeconds(wrongDisplaySeconds);
        if (openBookWrong) openBookWrong.SetActive(false);
        wrongRoutine = null;
    }
}