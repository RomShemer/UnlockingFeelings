using UnityEngine;

public class graffitiFade : MonoBehaviour
{
    public float fadeDuration = 2f;

    private Material mat;

    void Start()
    {
        // נניח שיש רק חומר אחד
        mat = GetComponent<Renderer>().material;
        StartCoroutine(FadeOut());
    }

    private System.Collections.IEnumerator FadeOut()
    {
        Color startColor = mat.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mat.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Destroy(gameObject, 2f); // מוחק את האובייקט בסיום
    }
}
