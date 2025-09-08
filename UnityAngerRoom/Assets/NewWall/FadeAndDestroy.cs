using UnityEngine;

public class FadeAndDestroy : MonoBehaviour
{
    public float baseDelay = 1f;   
    public float randomExtraDelay = 3f; 
    public float fadeDuration = 2f;

    private Material mat;
    private Color originalColor;
    private float timer = 0f;
    private bool fading = false;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        originalColor = mat.color;

        float totalDelay = baseDelay + Random.Range(0f, randomExtraDelay);
        Invoke(nameof(StartFading), totalDelay);
    }

    void StartFading()
    {
        fading = true;
    }

    void Update()
    {
        if (!fading) return;

        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(originalColor.a, 0f, timer / fadeDuration);
        mat.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}