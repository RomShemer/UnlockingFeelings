using System.Collections;
using UnityEngine;

public class BookPageTurnSimple : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public MeshRenderer pagesRenderer;     // ה-MeshRenderer של "book content"
    public Transform pagesPivot;           // אם ריק נשתמש ב-transform של pagesRenderer

    [Header("Flip Settings")]
    public float flipDuration = 0.7f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Preview")]
    public Texture2D previewTexture;       // לבדיקה מהאינספקטור

    Coroutine running;

    public void FlipTo(Texture2D newSpread)
    {
        if (!gameObject.activeInHierarchy) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FlipRoutine(newSpread));
    }

    IEnumerator FlipRoutine(Texture2D newSpread)
    {
        if (pagesRenderer == null) yield break;
        if (pagesPivot == null) pagesPivot = pagesRenderer.transform;

        var start = pagesPivot.localScale;
        var mid   = new Vector3(0.01f, start.y, start.z); // כמעט 0 כדי לא לקרוס UVs
        float half = flipDuration * 0.5f;

        // חצי ראשון: 1 -> 0
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            float k = curve.Evaluate(t / half);
            pagesPivot.localScale = Vector3.Lerp(start, mid, k);
            yield return null;
        }
        pagesPivot.localScale = mid;

        // החלפת הטקסטורה באמצע
        var mat = pagesRenderer.material; // אינסטנס כדי לא להשפיע על חומרים אחרים
        int baseMap = mat.HasProperty("_BaseMap") ? Shader.PropertyToID("_BaseMap")
                                                  : Shader.PropertyToID("_MainTex");
        mat.SetTexture(baseMap, newSpread);

        // חצי שני: 0 -> 1
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            float k = curve.Evaluate(t / half);
            pagesPivot.localScale = Vector3.Lerp(mid, start, k);
            yield return null;
        }
        pagesPivot.localScale = start;
        running = null;
    }

    [ContextMenu("TEST: Flip To Preview")]
    void TestFlip() { if (previewTexture) FlipTo(previewTexture); }
}
