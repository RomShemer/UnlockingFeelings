using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class BillboardPrompt : MonoBehaviour
{
    [Header("References")]
    public Transform playerHead;          // גרור את המצלמה (XR Main Camera / CenterEye)

    [Header("Behavior")]
    public float showDistance = 2.0f;     // מתי להציג (מטרים)
    public float fadeSpeed = 6f;          // מהירות דהייה

    CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        // בהתחלה נסתר בעדינות
        if (cg != null) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
    }

    void Update()
    {
        if (!playerHead || !cg) return;

        // תמיד מסתכל אל הראש (בלי להתהפך)
        Vector3 toHead = playerHead.position - transform.position;
        Vector3 flat = Vector3.ProjectOnPlane(toHead, Vector3.up);
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

        // הצגה/הסתרה לפי מרחק
        float d = toHead.magnitude;
        bool shouldShow = d <= showDistance;

        float target = shouldShow ? 1f : 0f;
        cg.alpha = Mathf.MoveTowards(cg.alpha, target, Time.deltaTime * fadeSpeed);
        cg.interactable = cg.alpha > 0.99f;
        cg.blocksRaycasts = cg.interactable;
    }
}