using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    Transform cam;

    [Tooltip("סובב רק סביב ציר Y (נוח לתוויות מעל קלידים)")]
    public bool onlyYaw = true;

    [Tooltip("תיקון היפוך/מראה במקרים של סקייל שלילי בשרשרת ההורים")]
    public bool fixMirror = true;

    void Start()
    {
        if (Camera.main) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!cam)
        {
            var c = Camera.main;
            if (!c) return;
            cam = c.transform;
        }

        // נעול לזווית ה-Y של המצלמה (מונע היפוכים מוזרים)
        if (onlyYaw)
        {
            var e = transform.eulerAngles;
            e.y = cam.eulerAngles.y;
            transform.eulerAngles = e;
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(cam.position - transform.position, Vector3.up);
        }

        if (fixMirror)
        {
            // ודא שהסקייל המקומי חיובי (מונע טקסט במראה)
            var ls = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
        }
    }
}