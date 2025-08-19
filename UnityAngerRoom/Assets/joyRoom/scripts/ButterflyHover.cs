using UnityEngine;

public class ButterflyHover : MonoBehaviour
{
    [Header("Two wing poses (two child objects)")]
    public Transform wingsPoseA;   // לדוגמה: Wings_Side1_...
    public Transform wingsPoseB;   // לדוגמה: Wings_Top1_...

    [Header("Flap")]
    public float flapSpeed = 12f;  // מהירות הנפנוף
    public float sharpness = 2.2f; // חדות המעבר (2–4 טוב)
    public bool randomizePhase = true;

    float phase;

    void Awake()
    {
        if (randomizePhase) phase = Random.value * 10f;
        if (wingsPoseA) wingsPoseA.localScale = Vector3.one;
        if (wingsPoseB) wingsPoseB.localScale = Vector3.zero;
    }

    void Update()
    {
        float t = Mathf.Sin((Time.time + phase) * flapSpeed) * 0.5f + 0.5f;
        t = Mathf.Pow(t, sharpness); // מעבר חלק/חד יותר
        if (wingsPoseA) wingsPoseA.localScale = Vector3.one * (1f - t);
        if (wingsPoseB) wingsPoseB.localScale = Vector3.one * t;
    }
}
