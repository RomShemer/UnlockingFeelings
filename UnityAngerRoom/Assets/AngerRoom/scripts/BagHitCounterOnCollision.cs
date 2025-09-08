using UnityEngine;

public class BagHitCounterOnCollision : MonoBehaviour
{
    [Header("Who can hit")]
    public LayerMask hitterLayers;        // שכבה/ות של היד/פטיש
    public string[] allowedTags;          // או תגיות (לא חובה)

    [Header("Hit logic")]
    public int hitsToVanish = 3;
    public float minRelativeSpeed = 0.6f; // סף מהירות יחסית כדי להיחשב כמכה
    public float cooldown = 0.05f;        // כדי לא לספור פעמיים באותו מגע

    [Header("VFX/SFX (optional)")]
    public AudioSource audioSource;
    public AudioClip hitClip;

    int _hits = 0;
    float _lastHitTime = -999f;

    bool IsHitter(GameObject go)
    {
        if (((1 << go.layer) & hitterLayers) != 0) return true;
        if (allowedTags != null && allowedTags.Length > 0)
        {
            foreach (var t in allowedTags)
                if (!string.IsNullOrEmpty(t) && go.CompareTag(t)) return true;
        }
        return false;
    }

    void OnCollisionEnter(Collision c)
    {
        if (!IsHitter(c.gameObject)) return;

        // מהירות יחסית בין המכה לשק
        float relSpeed = c.relativeVelocity.magnitude;
        if (relSpeed < minRelativeSpeed) return;

        float now = Time.time;
        if (now - _lastHitTime < cooldown) return;
        _lastHitTime = now;

        _hits++;

        if (audioSource && hitClip)
        {
            audioSource.pitch = 0.9f + 0.2f * Mathf.Clamp01(relSpeed / (minRelativeSpeed * 2f));
            audioSource.PlayOneShot(hitClip);
        }

        if (_hits >= hitsToVanish)
        {
            gameObject.SetActive(false);   // מעלים את השק
        }
    }
}
